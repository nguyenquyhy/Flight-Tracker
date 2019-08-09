using FlightTracker.DTOs;
using System;
using System.Collections.Generic;

namespace FlightTracker.Clients.Logics
{
    public class AirportListUpdatedEventArgs : EventArgs
    {
        public IEnumerable<AirportData> Airports { get; private set; }

        public AirportListUpdatedEventArgs(IEnumerable<AirportData> airports)
        {
            Airports = airports;
        }
    }

    public interface IAirportUpdater
    {
        event EventHandler<AirportListUpdatedEventArgs> AirportListUpdated;
    }
}
