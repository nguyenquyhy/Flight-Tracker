using FlightTracker.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class SignalRLogic
    {
        private HubConnection connection;
        private FlightStatus lastStatus;
        private readonly ILogger<SignalRLogic> logger;

        public SignalRLogic(ILogger<SignalRLogic> logger,
            IOptions<AppSettings> settings,
            IAircraftDataUpdater aircraftDataUpdater,
            IFlightPlanUpdater flightPlanUpdater,
            IFlightStatusUpdater flightStatusUpdater)
        {
            connection = new HubConnectionBuilder()
                .WithUrl(settings.Value.BaseUrl + "/Hubs/Status")
                .WithAutomaticReconnect()
                .Build();
            this.logger = logger;

            aircraftDataUpdater.AircraftDataUpdated += AircraftDataUpdater_AircraftDataUpdated;
            flightPlanUpdater.FlightPlanUpdated += FlightPlanUpdater_FlightPlanUpdated;
            flightStatusUpdater.FlightStatusUpdated += FlightStatusUpdater_FlightStatusUpdated;
        }

        private async void AircraftDataUpdater_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                    await connection.InvokeAsync("Set", e.Data);
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot send to SignalR!", ex);
            }
        }

        private async void FlightPlanUpdater_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                    await connection.InvokeAsync("FlightPlan", e.FlightPlan);
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot send to SignalR!", ex);
            }
        }

        private async void FlightStatusUpdater_FlightStatusUpdated(object sender, FlightStatusUpdatedEventArgs e)
        {
            if (lastStatus != null)
            {
                // Try to reduce the number of status
                if (e.FlightStatus.IsOnGround && e.FlightStatus.GroundSpeed < 1f && lastStatus.IsOnGround && lastStatus.GroundSpeed < 1f)
                    return;
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

            try
            {
                if (connection.State == HubConnectionState.Connected)
                    await connection.InvokeAsync("Update", e.FlightStatus);
                lastStatus = e.FlightStatus;
            }
            catch (Exception ex)
            {
                logger.LogError("Cannot send to SignalR!", ex);
            }
        }

        public async Task StartAsync()
        {
            await connection.StartAsync();
            logger.LogInformation("Connection started");
        }
    }
}
