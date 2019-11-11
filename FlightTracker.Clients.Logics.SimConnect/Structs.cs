using System.Runtime.InteropServices;

namespace FlightTracker.Clients.Logics
{
    enum EVENTS
    {
        SIM_START,
        SIM_STOP,
        PAUSED,
        AIRCRAFT_LOADED,
        FLIGHT_LOADED,
        CRASHED,
        CRASH_RESET,
        FLIGHTPLAN_ACTIVATED,
        FLIGHTPLAN_DEACTIVATED,
        POSITION_CHANGED,
        SCREENSHOT,

        SIM_RATE_INCREASE,
        SIM_RATE_DECREASE
    };

    enum GROUPID
    {
        FLAG = 2000000000,
    };

    enum DEFINITIONS
    {
        PlaneData,
        FlightPlan,
        FlightStatus,
        EnvironmentData
    }

    internal enum DATA_REQUESTS
    {
        NONE,
        SUBSCRIBE_GENERIC,
        AIRCRAFT_DATA,
        FLIGHT_PLAN,
        FLIGHT_PLAN_STATUS,
        FLIGHT_STATUS,
        ENVIRONMENT_DATA
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct EnvironmentDataStruct
    {
        public int LocalTime;
        public int ZuluTime;
        public long AbsoluteTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct AircraftDataStruct
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcModel;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string AtcAirline;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AtcFlightNumber;

        public double FuelTotalCapacity { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FlightPlanStruct
    {
        public int NumberOfWaypoints;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ApproachingAirport;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct FlightStatusStruct
    {
        public double SimTime;
        public int SimRate;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double AltitudeAboveGround;
        public double Pitch;
        public double Bank;
        public double TrueHeading;
        public double MagneticHeading;
        public double GroundAltitude;
        public double GroundSpeed;
        public double IndicatedAirSpeed;
        public double VerticalSpeed;

        public double FuelTotalQuantity;

        public double WindVelocity;
        public double WindDirection;

        public int IsOnGround;
        public int StallWarning;
        public int OverspeedWarning;

        public int IsAutopilotOn;
    }
}