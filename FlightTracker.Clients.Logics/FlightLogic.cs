using FlightTracker.DTOs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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
        private bool forceStatusAdd = false;

        public FlightData FlightData { get; private set; } = new FlightData();

        public List<ClientFlightStatus> FlightRoute { get; private set; } = new List<ClientFlightStatus>();

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
            await File.WriteAllTextAsync(dataFile, JsonConvert.SerializeObject(FlightData));
            await File.WriteAllTextAsync(routeFile, JsonConvert.SerializeObject(FlightRoute));
        }

        public async Task<bool> SaveAsync()
        {
            if (FlightData != null)
            {
                logger.LogInformation("Saving flight manually");
                var result = await AddOrUpdateFlightAsync(true);
                logger.LogInformation(result ? "Saved current flight" : "Cannot save current flight");
                return result;
            }
            return false;
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
            if (FlightData != null && FlightData.State != FlightState.Started && FlightData.Id != null)
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
            FlightData = new FlightData
            {
                Title = title,
                StartDateTime = DateTimeOffset.Now,
                Aircraft = FlightData?.Aircraft,
                FlightPlan = FlightData?.FlightPlan,
                Airline = FlightData?.Aircraft?.Airline,
                FlightNumber = FlightData?.Aircraft?.FlightNumber
            };
            FlightRoute = new List<ClientFlightStatus>();
            HandleFlightDataUpdated();
        }

        private bool updated;
        private void HandleFlightDataUpdated()
        {
            updated = true;
            FlightUpdated?.Invoke(this, new FlightUpdatedEventArgs(FlightData));
        }

        private readonly ConcurrentQueue<string> screenshotsQueue = new ConcurrentQueue<string>();

        public async Task<bool> UploadScreenshotAsync(string name, byte[] image)
        {
            try
            {
                if (FlightRoute.Count > 0)
                {
                    var url = await imageUploader.UploadAsync(name, image);
                    screenshotsQueue.Enqueue(url);
                    return true;
                }
                else
                {
                    logger.LogInformation("Screenshot is skipped because flight is not started.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot upload screenshot {0}", name);
                return false;
            }
        }

        public void UpdateTitle(string title)
        {
            FlightData.Title = title;
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

        private async void FlightSimInterface_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            if (FlightData.State == FlightState.Started)
            {
                FlightData.Aircraft = e.Data;
                HandleFlightDataUpdated();
            }
            else
            {
                await NewFlightAsync(NewFlightReason.UserRequest, FlightData.Title);
                FlightData.Aircraft = e.Data;
                HandleFlightDataUpdated();
            }
        }

        public void AddNextStatus()
        {
            forceStatusAdd = true;
        }

        private void FlightSimInterface_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            FlightData.FlightPlan = e.FlightPlan;
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
                if (screenshotsQueue.TryDequeue(out var screenshot))
                {
                    e.FlightStatus.ScreenshotUrl = screenshot;
                }

                var lastStatus = FlightRoute.LastOrDefault();

                if (lastStatus != null && lastStatus.IsOnGround && !e.FlightStatus.IsOnGround && FlightData.State == FlightState.Started)
                {
                    // Took off
                    logger.LogInformation("Aircraft has taken off");
                    FlightData.StatusTakeOff = e.FlightStatus;
                    FlightData.State = FlightState.Enroute;

                    FlightData.TakeOffDateTime = DateTimeOffset.Now;

                    // Try to determine airport
                    if (airports != null && FlightData.AirportFrom == null)
                    {
                        FlightData.AirportFrom = GetNearestAirport(e);
                    }

                    HandleFlightDataUpdated();

                    await AddOrUpdateFlightAsync();
                }
                else if (lastStatus != null && !lastStatus.IsOnGround && e.FlightStatus.IsOnGround && FlightData.StatusLanding == null)
                {
                    // Landing
                    if (FlightData.State == FlightState.Enroute)
                    {
                        logger.LogInformation("Aircraft has landed");
                        FlightData.StatusLanding = lastStatus;
                        FlightData.State = FlightState.Arrived;

                        if (airports != null && FlightData.AirportTo == null)
                        {
                            FlightData.AirportTo = GetNearestAirport(e);
                        }

                        HandleFlightDataUpdated();
                    }

                    await AddOrUpdateFlightAsync();
                }
                else
                {
                    if (!e.FlightStatus.IsOnGround && FlightData.State == FlightState.Started && e.FlightStatus.GroundSpeed > 5)
                    {
                        // Flight start when the airplane is already flying
                        FlightData.State = FlightState.Enroute;
                        HandleFlightDataUpdated();
                    }

                    // Try to reduce the number of status by skipping status recording
                    if (!forceStatusAdd && string.IsNullOrEmpty(screenshot))
                    {
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
                }

                FlightRoute.Add(e.FlightStatus);
                forceStatusAdd = false;

                if ((FlightData.State == FlightState.Enroute || FlightData.State == FlightState.Arrived)
                    && (!lastSaveAttempt.HasValue || DateTime.Now - lastSaveAttempt.Value > TimeSpan.FromMilliseconds(SaveDelay)))
                {
                    // Throttle save attempt
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
            if (FlightData != null)
            {
                logger.LogInformation("Aircraft has crashed");
                FlightData.State = FlightState.Crashed;
                FlightData.AirportTo = null;
                
                HandleFlightDataUpdated();

                await NewFlightAsync(NewFlightReason.Crashed, FlightData.Title);
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
            if (FlightData != null)
            {
                if (FlightData.State == FlightState.Enroute)
                {
                    logger.LogInformation("Set flight status to lost");
                    FlightData.State = FlightState.Lost;

                    HandleFlightDataUpdated();
                }
                await NewFlightAsync(NewFlightReason.Closed, FlightData.Title);
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
                if (FlightData.Id == null)
                {
                    var newData = await flightsAPIClient.AddFlightAsync(FlightData);

                    // Copy data since it might be changed already;
                    newData.Title = FlightData.Title;
                    newData.Aircraft = FlightData.Aircraft;
                    newData.FlightPlan = FlightData.FlightPlan;

                    FlightData = newData;
                    HandleFlightDataUpdated();

                    if (FlightRoute.Any())
                    {
                        await flightsAPIClient.PostRouteAsync(FlightData.Id, FlightRoute.Cast<FlightStatus>().ToList());
                    }
                }
                else
                {
                    if (updated)
                    {
                        await flightsAPIClient.UpdateFlightAsync(FlightData.Id, FlightData);
                    }
                    if (FlightRoute.Any())
                    {
                        await flightsAPIClient.PostRouteAsync(FlightData.Id, FlightRoute.Cast<FlightStatus>().ToList());
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
