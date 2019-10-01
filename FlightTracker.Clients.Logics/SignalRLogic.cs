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
            IFlightSimInterface flightSimInterface,
            FlightLogic flightLogic)
        {
            connection = new HubConnectionBuilder()
                .WithUrl(settings.Value.BaseUrl + "/Hubs/Status")
                .WithAutomaticReconnect()
                .Build();
            this.logger = logger;

            flightSimInterface.Closed += FlightSimInterface_Closed;
            flightSimInterface.FlightStatusUpdated += FlightSimInterface_FlightStatusUpdated;
            flightLogic.FlightUpdated += FlightLogic_FlightUpdated;

            Task.Factory.StartNew(PeriodicSendAsync);
        }

        private void FlightLogic_FlightUpdated(object sender, FlightUpdatedEventArgs e)
        {
            updatedFlight = e.FlightData;
        }

        private FlightData updatedFlight = null;
        private async Task PeriodicSendAsync()
        {
            while (true)
            {
                if (updatedFlight != null && connection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await connection.InvokeAsync("Set", updatedFlight);
                        updatedFlight = null;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Cannot send to SignalR!");
                    }
                }
                await Task.Delay(5000);
            }
        }

        private async void FlightSimInterface_Closed(object sender, EventArgs e)
        {
            await connection.InvokeAsync("Set", null);
            updatedFlight = null;
        }

        private async void FlightSimInterface_FlightStatusUpdated(object sender, FlightStatusUpdatedEventArgs e)
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
                logger.LogError(ex, "Cannot send to SignalR!");
            }
        }

        public async Task StartAsync()
        {
            await connection.StartAsync();
            logger.LogInformation("Connection started");
        }
    }
}
