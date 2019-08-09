using System;

namespace FlightTracker.Clients.Logics
{
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

    public interface IEnvironmentDataUpdater
    {
        event EventHandler<EnvironmentDataUpdatedEventArgs> EnvironmentDataUpdated;
    }
}
