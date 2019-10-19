using FlightTracker.DTOs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class FlightUpdatedEventArgs : EventArgs
    {
        public FlightUpdatedEventArgs(FlightData flightData)
        {
            FlightData = flightData;
        }

        public FlightData FlightData { get; }
    }

    public class FlightLogic
    {
        private const int SaveDelay = 5000;

        private readonly ILogger<FlightLogic> logger;
        private readonly FlightsAPIClient flightsAPIClient;
        private readonly IImageUploader imageUploader;
        private TaskCompletionSource<bool> tcsCrashReset = null;

        private FlightData flightData = new FlightData();
        private List<FlightStatus> flightRoute = new List<FlightStatus>();

        private readonly List<AirportData> airports = new List<AirportData>();

        int? localTime = null;
        int? zuluTime = null;
        long? absoluteTime = null;

        DateTime? lastSaveAttempt = null;

        public event EventHandler<FlightUpdatedEventArgs> FlightUpdated;

        public FlightLogic(ILogger<FlightLogic> logger,
            FlightsAPIClient flightsAPIClient,
            IFlightSimInterface flightSimInterface,
            IImageUploader imageUploader)
        {
            this.logger = logger;
            this.flightsAPIClient = flightsAPIClient;
            this.imageUploader = imageUploader;
            flightSimInterface.EnvironmentDataUpdated += FlightSimInterface_EnvironmentDataUpdated;
            flightSimInterface.AirportListUpdated += FlightSimInterface_AirportListUpdated;
            flightSimInterface.AircraftDataUpdated += FlightSimInterface_AircraftDataUpdated;
            flightSimInterface.FlightPlanUpdated += FlightSimInterface_FlightPlanUpdated;

            flightSimInterface.FlightStatusUpdated += FlightSimInterface_FlightStatusUpdated;
            flightSimInterface.Crashed += FlightSimInterface_CrashedAsync;
            flightSimInterface.CrashReset += FlightSimInterface_CrashReset;
            flightSimInterface.Closed += FlightSimInterface_Closed;
        }

        public async Task DumpAsync()
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var dataFile = Path.Combine(Directory.GetCurrentDirectory(), "flightdata-" + now + ".json");
            var routeFile = Path.Combine(Directory.GetCurrentDirectory(), "flightroute-" + now + ".json");
            await File.WriteAllTextAsync(dataFile, JsonConvert.SerializeObject(flightData));
            await File.WriteAllTextAsync(routeFile, JsonConvert.SerializeObject(flightRoute));
        }

        public async Task SaveAsync()
        {
            if (flightData != null)
            {
                logger.LogInformation("Saving flight manually");
                var result = await AddOrUpdateFlightAsync(true);
                logger.LogInformation(result ? "Saved current flight" : "Cannot save current flight");
            }
        }

        public enum NewFlightReason
        {
            UserRequest,
            Crashed,
            Closed
        }

        public async Task NewFlightAsync(NewFlightReason reason, string title)
        {
            // TODO: Flush current flight
            if (flightData != null && flightData.State != FlightState.Started && flightData.Id != null)
            {
                logger.LogInformation("Trying to save existing flight");
                var result = await AddOrUpdateFlightAsync();
                logger.LogInformation(result ? "Saved existing flight" : "Cannot save existing flight");
            }

            if (reason == NewFlightReason.Crashed)
            {
                tcsCrashReset = new TaskCompletionSource<bool>();
                await tcsCrashReset.Task;
                await Task.Delay(5000);
            }

            lastSaveAttempt = null;
            flightData = new FlightData
            {
                Title = title,
                StartDateTime = DateTimeOffset.Now,
                Aircraft = flightData?.Aircraft,
                FlightPlan = flightData?.FlightPlan,
                Airline = flightData?.Aircraft?.Airline,
                FlightNumber = flightData?.Aircraft?.FlightNumber
            };
            flightRoute = new List<FlightStatus>();
            HandleFlightDataUpdated();
        }

        private bool updated;
        private void HandleFlightDataUpdated()
        {
            updated = true;
            FlightUpdated?.Invoke(this, new FlightUpdatedEventArgs(flightData));
        }

        public async Task ScreenshotAsync(string name, byte[] image)
        {
            try
            {
                var lastStatus = flightRoute.LastOrDefault();
                if (lastStatus != null)
                {
                    var url = await imageUploader.UploadAsync(name, image);
                    lastStatus.ScreenshotUrl = url;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot upload screenshot {0}", name);
            }
        }

        public void UpdateTitle(string title)
        {
            flightData.Title = title;
            HandleFlightDataUpdated();
        }

        private void FlightSimInterface_EnvironmentDataUpdated(object sender, EnvironmentDataUpdatedEventArgs e)
        {
            localTime = e.LocalTime;
            zuluTime = e.ZuluTime;
            absoluteTime = e.AbsoluteTime;
        }

        private void FlightSimInterface_AirportListUpdated(object sender, AirportListUpdatedEventArgs e)
        {
            logger.LogInformation("Receive Airport: " + string.Join(", ", e.Airports.Select(o => o.Icao)));
            foreach (var airport in e.Airports)
            {
                if (!airports.Any(a => a.Icao == airport.Icao))
                {
                    airports.Add(airport);
                }
            }
        }

        private void FlightSimInterface_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            flightData.Aircraft = e.Data;
            HandleFlightDataUpdated();
        }

        private void FlightSimInterface_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            flightData.FlightPlan = e.FlightPlan;
            HandleFlightDataUpdated();
        }

        private async void FlightSimInterface_FlightStatusUpdated(object sender, FlightStatusUpdatedEventArgs e)
        {
            try
            {
                // Augment environment time
                e.FlightStatus.LocalTime = localTime;
                e.FlightStatus.ZuluTime = zuluTime;
                e.FlightStatus.AbsoluteTime = absoluteTime;

                var lastStatus = flightRoute.LastOrDefault();

                if (lastStatus != null && lastStatus.IsOnGround && !e.FlightStatus.IsOnGround && flightData.State == FlightState.Started)
                {
                    // Took off
                    logger.LogInformation("Aircraft has taken off");
                    flightData.StatusTakeOff = e.FlightStatus;
                    flightData.State = FlightState.Enroute;

                    flightData.TakeOffDateTime = DateTimeOffset.Now;

                    // Try to determine airport
                    if (airports != null && flightData.AirportFrom == null)
                    {
                        flightData.AirportFrom = GetNearestAirport(e);
                    }

                    HandleFlightDataUpdated();

                    await AddOrUpdateFlightAsync();
                }
                else if (lastStatus != null && !lastStatus.IsOnGround && e.FlightStatus.IsOnGround && flightData.StatusLanding == null)
                {
                    // Landing
                    if (flightData.State == FlightState.Enroute)
                    {
                        logger.LogInformation("Aircraft has landed");
                        flightData.StatusLanding = lastStatus;
                        flightData.State = FlightState.Arrived;

                        if (airports != null && flightData.AirportTo == null)
                        {
                            flightData.AirportTo = GetNearestAirport(e);
                        }

                        HandleFlightDataUpdated();
                    }

                    await AddOrUpdateFlightAsync();
                }
                else
                {
                    if (!e.FlightStatus.IsOnGround && flightData.State == FlightState.Started && e.FlightStatus.GroundSpeed > 5)
                    {
                        // Flight start when the airplane is already flying
                        flightData.State = FlightState.Enroute;
                        HandleFlightDataUpdated();
                    }

                    // Try to reduce the number of status
                    if (lastStatus == null && e.FlightStatus.IsOnGround && e.FlightStatus.GroundSpeed < 1f)
                        return;
                    else if (lastStatus != null && lastStatus.ScreenshotUrl == null)
                    {
                        if (e.FlightStatus.AltitudeAboveGround > 1000 && e.FlightStatus.SimTime - lastStatus.SimTime < 1f)
                            return;
                        if (e.FlightStatus.AltitudeAboveGround > 5000 && e.FlightStatus.SimTime - lastStatus.SimTime < 2f)
                            return;
                        if (e.FlightStatus.AltitudeAboveGround > 10000 && e.FlightStatus.SimTime - lastStatus.SimTime < 3f)
                            return;
                        if (e.FlightStatus.AltitudeAboveGround > 15000 && e.FlightStatus.SimTime - lastStatus.SimTime < 4f)
                            return;
                        if (e.FlightStatus.AltitudeAboveGround > 20000 && e.FlightStatus.SimTime - lastStatus.SimTime < 5f)
                            return;
                    }
                }

                flightRoute.Add(e.FlightStatus);

                if ((flightData.State == FlightState.Enroute || flightData.State == FlightState.Arrived)
                    && (!lastSaveAttempt.HasValue || DateTime.Now - lastSaveAttempt.Value > TimeSpan.FromMilliseconds(SaveDelay)))
                {
                    await AddOrUpdateFlightAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot process flight status update: {0}", ex.Message);
            }
        }

        private async void FlightSimInterface_CrashedAsync(object sender, EventArgs e)
        {
            if (flightData != null)
            {
                logger.LogInformation("Aircraft has crashed");
                flightData.State = FlightState.Crashed;
                flightData.AirportTo = null;
                
                HandleFlightDataUpdated();

                await NewFlightAsync(NewFlightReason.Crashed, flightData.Title);
            }
        }

        private void FlightSimInterface_CrashReset(object sender, EventArgs e)
        {
            if (tcsCrashReset != null)
            {
                tcsCrashReset.SetResult(true);
            }
        }

        private async void FlightSimInterface_Closed(object sender, EventArgs e)
        {
            if (flightData != null)
            {
                if (flightData.State == FlightState.Enroute)
                {
                    logger.LogInformation("Set flight status to lost");
                    flightData.State = FlightState.Lost;

                    HandleFlightDataUpdated();
                }
                await NewFlightAsync(NewFlightReason.Closed, flightData.Title);
            }
        }

        public bool IsSaving { get; private set; }
        private bool isLate = false;

        private async Task<bool> AddOrUpdateFlightAsync(bool force = false)
        {
            if (!force && IsSaving)
            {
                if (!isLate)
                {
                    isLate = true;
                    logger.LogInformation("Last save is still running. Skip this save!");
                }
                return false;
            }

            try
            {
                IsSaving = true;
                lastSaveAttempt = DateTime.Now;

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                if (flightData.Id == null)
                {
                    var newData = await flightsAPIClient.AddFlightAsync(flightData);

                    // Copy data since it might be changed already;
                    newData.Title = flightData.Title;
                    newData.Aircraft = flightData.Aircraft;
                    newData.FlightPlan = flightData.FlightPlan;

                    flightData = newData;
                    HandleFlightDataUpdated();

                    if (flightRoute.Any())
                    {
                        await flightsAPIClient.PostRouteAsync(flightData.Id, flightRoute);
                    }
                }
                else
                {
                    if (updated)
                    {
                        await flightsAPIClient.UpdateFlightAsync(flightData.Id, flightData);
                    }
                    if (flightRoute.Any())
                    {
                        await flightsAPIClient.PostRouteAsync(flightData.Id, flightRoute);
                    }
                }
                
                if (isLate)
                {
                    isLate = false;
                    logger.LogWarning($"This save took {stopWatch.ElapsedMilliseconds}ms!");
                }
                stopWatch.Stop();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, $"Cannot add/update flight! Error: {ex.GetType().FullName} {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, $"Cannot add/update flight! Error: {ex.GetType().FullName} {ex.Message}");
                return false;
            }
            finally
            {
                IsSaving = false;
            }

            return true;
        }

        private string GetNearestAirport(FlightStatusUpdatedEventArgs e)
        {
            string nearestIcao = null;
            var minDist = double.MaxValue;
            foreach (var airport in airports.ToList())
            {
                var dist = GpsHelper.CalculateDistance(
                    e.FlightStatus.Latitude, e.FlightStatus.Longitude, e.FlightStatus.Altitude,
                    airport.Latitude, airport.Longitude, airport.Altitude);
                if (dist < minDist)
                {
                    nearestIcao = airport.Icao;
                    minDist = dist;
                }
            }

            return nearestIcao;
        }
    }
}
