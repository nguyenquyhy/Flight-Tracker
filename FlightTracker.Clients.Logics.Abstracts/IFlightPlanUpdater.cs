using FlightTracker.DTOs;
using System;

namespace FlightTracker.Clients.Logics
{
    public class FlightPlanUpdatedEventArgs : EventArgs
    {
        public FlightPlan FlightPlan { get; private set; }

        public FlightPlanUpdatedEventArgs(FlightPlan flightPlan)
        {
            FlightPlan = flightPlan;
        }
    }

    public interface IFlightPlanUpdater
    {
        event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
    }
}
