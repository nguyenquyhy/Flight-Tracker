using FlightTracker.DTOs;
using System;

namespace FlightTracker.Clients.Logics
{
    public class FlightStatusUpdatedEventArgs : EventArgs
    {
        public FlightStatus FlightStatus { get; private set; }

        public FlightStatusUpdatedEventArgs(FlightStatus flightStatus)
        {
            FlightStatus = flightStatus;
        }
    }

    public interface IFlightStatusUpdater
    {
        event EventHandler<FlightStatusUpdatedEventArgs> FlightStatusUpdated;
        event EventHandler Crashed;
        event EventHandler CrashReset;

        void Screenshot();
    }
}
