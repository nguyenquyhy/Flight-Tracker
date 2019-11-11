using FlightTracker.DTOs;
using System;
using System.Collections.Generic;

namespace FlightTracker.Clients.Logics
{
    public interface IFlightSimInterface
    {
        event EventHandler<AirportListUpdatedEventArgs> AirportListUpdated;
        event EventHandler<EnvironmentDataUpdatedEventArgs> EnvironmentDataUpdated;

        event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;

        event EventHandler<FlightStatusUpdatedEventArgs> FlightStatusUpdated;
        event EventHandler Crashed;
        event EventHandler CrashReset;
        event EventHandler Closed;

        void Screenshot();
        void IncreaseSimRate();
        void DecreaseSimRate();
    }

    public class AircraftDataUpdatedEventArgs : EventArgs
    {
        public AircraftData Data { get; private set; }

        public AircraftDataUpdatedEventArgs(AircraftData data)
        {
            Data = data;
        }
    }

    public class AirportListUpdatedEventArgs : EventArgs
    {
        public IEnumerable<AirportData> Airports { get; private set; }

        public AirportListUpdatedEventArgs(IEnumerable<AirportData> airports)
        {
            Airports = airports;
        }
    }

    public class EnvironmentDataUpdatedEventArgs : EventArgs
    {
        public int LocalTime { get; set; }
        public int ZuluTime { get; set; }
        public long AbsoluteTime { get; set; }

        public EnvironmentDataUpdatedEventArgs(int localTime, int zuluTime, long absoluteTime)
        {
            LocalTime = localTime;
            ZuluTime = zuluTime;
            AbsoluteTime = absoluteTime;
        }
    }

    public class FlightPlanUpdatedEventArgs : EventArgs
    {
        public FlightPlan FlightPlan { get; private set; }

        public FlightPlanUpdatedEventArgs(FlightPlan flightPlan)
        {
            FlightPlan = flightPlan;
        }
    }

    public class FlightStatusUpdatedEventArgs : EventArgs
    {
        public ClientFlightStatus FlightStatus { get; private set; }

        public FlightStatusUpdatedEventArgs(ClientFlightStatus flightStatus)
        {
            FlightStatus = flightStatus;
        }
    }
}
