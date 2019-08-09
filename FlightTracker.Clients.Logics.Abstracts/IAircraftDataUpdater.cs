using FlightTracker.DTOs;
using System;

namespace FlightTracker.Clients.Logics
{
    public class AircraftDataUpdatedEventArgs : EventArgs
    {
        public AircraftData Data { get; private set; }

        public AircraftDataUpdatedEventArgs(AircraftData data)
        {
            Data = data;
        }
    }

    public interface IAircraftDataUpdater
    {
        event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
    }
}
