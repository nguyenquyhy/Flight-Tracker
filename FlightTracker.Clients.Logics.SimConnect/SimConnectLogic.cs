using FlightTracker.DTOs;
using LockheedMartin.Prepar3D.SimConnect;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class SimConnectLogic : IFlightSimInterface
    {
        private const int StatusDelayMilliseconds = 500;

        public event EventHandler<EnvironmentDataUpdatedEventArgs> EnvironmentDataUpdated;
        public event EventHandler<AircraftDataUpdatedEventArgs> AircraftDataUpdated;
        public event EventHandler<FlightPlanUpdatedEventArgs> FlightPlanUpdated;
        public event EventHandler<AirportListUpdatedEventArgs> AirportListUpdated;

        public event EventHandler<FlightStatusUpdatedEventArgs> FlightStatusUpdated;
        public event EventHandler Crashed;
        public event EventHandler CrashReset;

        public event EventHandler Closed;

        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;
        private readonly ILogger<SimConnectLogic> logger;

        public IntPtr Handle { get; private set; }

        private SimConnect simconnect = null;

        public SimConnectLogic(ILogger<SimConnectLogic> logger)
        {
            this.logger = logger;
        }

        // Simconnect client will send a win32 message when there is 
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.
        public IntPtr HandleSimConnectEvents(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            isHandled = false;

            switch (message)
            {
                case WM_USER_SIMCONNECT:
                    {
                        if (simconnect != null)
                        {
                            try
                            {
                                this.simconnect.ReceiveMessage();
                            }
                            catch { RecoverFromError(); }

                            isHandled = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return IntPtr.Zero;
        }

        // Set up the SimConnect event handlers
        public void Initialize(IntPtr Handle)
        {
            simconnect = new SimConnect("Flight Tracker", Handle, WM_USER_SIMCONNECT, null, 0);

            // listen to connect and quit msgs
            simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(simconnect_OnRecvOpen);
            simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(simconnect_OnRecvQuit);

            // listen to exceptions
            simconnect.OnRecvException += simconnect_OnRecvException;

            // catch the assigned object IDs
            simconnect.OnRecvSimobjectDataBytype += simconnect_OnRecvSimobjectDataBytypeAsync;
            RegisterAircraftDataDefinition();
            RegisterFlightPlanDefinition();
            RegisterFlightStatusDefinition();

            simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;
            RegisterEnvironmentDataDefinition();

            // Subscribe to system event Pause
            simconnect.SubscribeToSystemEvent(EVENTS.SIM_START, "SimStart");
            simconnect.SubscribeToSystemEvent(EVENTS.SIM_STOP, "SimStop");
            simconnect.SubscribeToSystemEvent(EVENTS.PAUSED, "Pause");
            simconnect.SubscribeToSystemEvent(EVENTS.AIRCRAFT_LOADED, "AircraftLoaded");
            simconnect.SubscribeToSystemEvent(EVENTS.FLIGHT_LOADED, "FlightLoaded");
            simconnect.SubscribeToSystemEvent(EVENTS.CRASHED, "Crashed");
            simconnect.SubscribeToSystemEvent(EVENTS.CRASH_RESET, "CrashReset");
            simconnect.SubscribeToSystemEvent(EVENTS.FLIGHTPLAN_ACTIVATED, "FlightPlanActivated");
            simconnect.SubscribeToSystemEvent(EVENTS.FLIGHTPLAN_DEACTIVATED, "FlightPlandeactivated");
            simconnect.SubscribeToSystemEvent(EVENTS.POSITION_CHANGED, "PositionChanged");
            simconnect.OnRecvEventFilename += Simconnect_OnRecvEventFilename;
            simconnect.OnRecvEvent += simconnect_OnRecvEvent;

            simconnect.OnRecvAirportList += simconnect_OnRecvAirportList;

            simconnect.OnRecvSystemState += simconnect_OnRecvSystemState;
            simconnect.RequestSystemState(DATA_REQUESTS.FLIGHT_PLAN, "FlightPlan");

            simconnect.MapClientEventToSimEvent(EVENTS.SCREENSHOT, "CAPTURE_SCREENSHOT");

            simconnect.MapClientEventToSimEvent(EVENTS.SIM_RATE_INCREASE, "SIM_RATE_INCR");
            simconnect.MapClientEventToSimEvent(EVENTS.SIM_RATE_DECREASE, "SIM_RATE_DECR");
        }

        public void CloseConnection()
        {
            try
            {
                if (simconnect != null)
                {
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.SIM_START);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.SIM_STOP);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.PAUSED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.AIRCRAFT_LOADED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.FLIGHT_LOADED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.CRASHED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.CRASH_RESET);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.FLIGHTPLAN_ACTIVATED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.FLIGHTPLAN_DEACTIVATED);
                    simconnect.UnsubscribeFromSystemEvent(EVENTS.POSITION_CHANGED);
                    simconnect.UnsubscribeToFacilities(SIMCONNECT_FACILITY_LIST_TYPE.AIRPORT);

                    // Dispose serves the same purpose as SimConnect_Close()
                    simconnect.Dispose();
                    simconnect = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Cannot unsubscribe events! Error: {ex.Message}");
            }
        }

        private void RegisterEnvironmentDataDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.EnvironmentData,
                "LOCAL TIME",
                "seconds",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.EnvironmentData,
                "ZULU TIME",
                "seconds",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.EnvironmentData,
                "ABSOLUTE TIME",
                "seconds",
                SIMCONNECT_DATATYPE.INT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<EnvironmentDataStruct>(DEFINITIONS.EnvironmentData);
        }

        private void RegisterFlightStatusDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIM TIME",
                "Seconds",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus, 
                "SIMULATION RATE", 
                "number", 
                SIMCONNECT_DATATYPE.INT32, 
                0.0f, 
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LATITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LONGITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALTITUDE",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALT ABOVE GROUND",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE PITCH DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE BANK DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES TRUE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES MAGNETIC",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND ALTITUDE",
                "Meters",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND VELOCITY",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AIRSPEED INDICATED",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "VERTICAL SPEED",
                "Feet per minute",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "FUEL TOTAL QUANTITY",
                "Gallons",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND VELOCITY",
                "Feet per second",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND DIRECTION",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIM ON GROUND",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "STALL WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "OVERSPEED WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT MASTER",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<FlightStatusStruct>(DEFINITIONS.FlightStatus);
        }

        private void RegisterAircraftDataDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "Title",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING256,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "ATC TYPE",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING32,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "ATC MODEL",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING32,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "ATC ID",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING32,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "ATC AIRLINE",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING64,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "ATC FLIGHT NUMBER",                        // Simulation Variable
                                                null,                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.STRING32,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.PlaneData,
                                                "FUEL TOTAL CAPACITY",                        // Simulation Variable
                                                "Gallons",                           // Units - for strings put 'null'
                                                SIMCONNECT_DATATYPE.FLOAT64,  // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<AircraftDataStruct>(DEFINITIONS.PlaneData);
        }

        private void RegisterFlightPlanDefinition()
        {
            //simconnect.AddToDataDefinition(DEFINITIONS.FlightPlan,
            //                                    "GPS IS ACTIVE FLIGHT PLAN",    // Simulation Variable
            //                                    "Bool",                    // Units - unitless "number". Note: use lowercase.
            //                                    SIMCONNECT_DATATYPE.INT64, // Data type
            //                                    0.0f,
            //                                    SimConnect.SIMCONNECT_UNUSED);

            //simconnect.AddToDataDefinition(DEFINITIONS.FlightPlan,
            //                                    "GPS IS DIRECTTO FLIGHTPLAN",    // Simulation Variable
            //                                    "Bool",                    // Units - unitless "number". Note: use lowercase.
            //                                    SIMCONNECT_DATATYPE.INT64, // Data type
            //                                    0.0f,
            //                                    SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightPlan,
                                                "GPS FLIGHT PLAN WP COUNT",    // Simulation Variable
                                                "number",                    // Units - unitless "number". Note: use lowercase.
                                                SIMCONNECT_DATATYPE.INT32, // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightPlan,
                                                "GPS APPROACH AIRPORT ID",    // Simulation Variable
                                                null,                    // Units - unitless "number". Note: use lowercase.
                                                SIMCONNECT_DATATYPE.STRING256, // Data type
                                                0.0f,
                                                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<FlightPlanStruct>(DEFINITIONS.FlightPlan);
        }

        public void Screenshot()
        {
            simconnect.TransmitClientEvent((uint)SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SCREENSHOT, (uint)0, GROUPID.FLAG, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        public void IncreaseSimRate()
        {
            simconnect.TransmitClientEvent((uint)SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SIM_RATE_INCREASE, 0, GROUPID.FLAG, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        public void DecreaseSimRate()
        {
            simconnect.TransmitClientEvent((uint)SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SIM_RATE_DECREASE, 0, GROUPID.FLAG, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            switch (data.dwRequestID)
            {
                case (uint)DATA_REQUESTS.ENVIRONMENT_DATA:
                    {
                        var envData = data.dwData[0] as EnvironmentDataStruct?;

                        if (envData.HasValue)
                        {
                            logger.LogDebug($"Local time: {envData.Value.LocalTime}. Zulu time: {envData.Value.ZuluTime}");

                            EnvironmentDataUpdated?.Invoke(this, new EnvironmentDataUpdatedEventArgs(
                                envData.Value.LocalTime,
                                envData.Value.ZuluTime,
                                envData.Value.AbsoluteTime
                                ));
                        }
                    }
                    break;
            }
        }

        private async void simconnect_OnRecvSimobjectDataBytypeAsync(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            // Must be general SimObject information
            switch (data.dwRequestID)
            {
                case (uint)DATA_REQUESTS.AIRCRAFT_DATA:
                    {
                        var aircraftData = data.dwData[0] as AircraftDataStruct?;

                        if (aircraftData.HasValue)
                        {
                            var dto = new AircraftData
                            {
                                Title = aircraftData.Value.Title,
                                Type = aircraftData.Value.AtcType,
                                Model = aircraftData.Value.AtcModel,
                                TailNumber = aircraftData.Value.AtcId,
                                Airline = aircraftData.Value.AtcAirline,
                                FlightNumber = aircraftData.Value.AtcFlightNumber
                            };

                            AircraftDataUpdated?.Invoke(this, new AircraftDataUpdatedEventArgs(dto));
                        }
                        else
                        {
                            // Cast failed
                            logger.LogError("Cannot cast to AircraftDataStruct!");
                        }
                    }
                    break;
                case (uint)DATA_REQUESTS.FLIGHT_PLAN_STATUS:
                    {
                        var flightPlan = data.dwData[0] as FlightPlanStruct?;
                    }
                    break;
                case (uint)DATA_REQUESTS.FLIGHT_STATUS:
                    {
                        var flightStatus = data.dwData[0] as FlightStatusStruct?;

                        if (flightStatus.HasValue)
                        {
                            FlightStatusUpdated?.Invoke(this, new FlightStatusUpdatedEventArgs(
                                new ClientFlightStatus
                                {
                                    SimTime = flightStatus.Value.SimTime,
                                    SimRate = flightStatus.Value.SimRate,
                                    Latitude = flightStatus.Value.Latitude,
                                    Longitude = flightStatus.Value.Longitude,
                                    Altitude = flightStatus.Value.Altitude,
                                    AltitudeAboveGround = flightStatus.Value.AltitudeAboveGround,
                                    Pitch = flightStatus.Value.Pitch,
                                    Bank = flightStatus.Value.Bank,
                                    Heading = flightStatus.Value.MagneticHeading,
                                    TrueHeading = flightStatus.Value.TrueHeading,
                                    GroundSpeed = flightStatus.Value.GroundSpeed,
                                    IndicatedAirSpeed = flightStatus.Value.IndicatedAirSpeed,
                                    VerticalSpeed = flightStatus.Value.VerticalSpeed,
                                    FuelTotalQuantity = flightStatus.Value.FuelTotalQuantity,
                                    IsOnGround = flightStatus.Value.IsOnGround == 1,
                                    StallWarning = flightStatus.Value.StallWarning == 1,
                                    OverspeedWarning = flightStatus.Value.OverspeedWarning == 1,
                                    IsAutopilotOn = flightStatus.Value.IsAutopilotOn == 1
                                }));
                        }
                        else
                        {
                            // Cast failed
                            logger.LogError("Cannot cast to FlightStatusStruct!");
                        }

                        await Task.Delay(StatusDelayMilliseconds);
                        simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.FLIGHT_STATUS, DEFINITIONS.FlightStatus, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                    }
                    break;
            }
        }

        private async void simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            switch (data.dwRequestID)
            {
                case (int)DATA_REQUESTS.FLIGHT_PLAN:
                    if (!string.IsNullOrEmpty(data.szString))
                    {
                        logger.LogInformation($"Receive flight plan {data.szString}");

                        var planName = data.szString;
                        var parser = new FlightPlanParser();
                        var plan = await parser.ParseAsync(planName);

                        var flightPlan = new FlightPlan
                        {
                            Title = plan.FlightPlanFlightPlan.Title,
                            Departure = new Point
                            {
                                Id = plan.FlightPlanFlightPlan.DepartureID,
                                Name = plan.FlightPlanFlightPlan.DepartureName,
                                Position = plan.FlightPlanFlightPlan.DeparturePosition,
                            },
                            Destination = new Point
                            {
                                Id = plan.FlightPlanFlightPlan.DestinationID,
                                Name = plan.FlightPlanFlightPlan.DestinationName,
                            },
                            CruisingAltitude = plan.FlightPlanFlightPlan.CruisingAlt
                        };
                        (flightPlan.Departure.Latitude, flightPlan.Departure.Longitude) = GpsHelper.ConvertString(plan.FlightPlanFlightPlan.DepartureLLA);
                        (flightPlan.Destination.Latitude, flightPlan.Destination.Longitude) = GpsHelper.ConvertString(plan.FlightPlanFlightPlan.DestinationLLA);

                        if (plan.FlightPlanFlightPlan.ATCWaypoint != null)
                        {
                            flightPlan.Waypoints = new System.Collections.Generic.List<Waypoint>();
                            foreach (var waypoint in plan.FlightPlanFlightPlan.ATCWaypoint)
                            {
                                var w = new Waypoint
                                {
                                    Id = waypoint.id,
                                    Type = waypoint.ATCWaypointType,
                                    Airway = waypoint.ATCAirway,
                                };
                                (w.Latitude, w.Longitude) = GpsHelper.ConvertString(waypoint.WorldPosition);
                                flightPlan.Waypoints.Add(w);
                            }
                        }

                        FlightPlanUpdated?.Invoke(this, new FlightPlanUpdatedEventArgs(flightPlan));
                    }
                    break;
            }
        }

        void simconnect_OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            logger.LogInformation("OnRecvEvent dwID " + data.dwID + " uEventID " + data.uEventID);
            switch ((SIMCONNECT_RECV_ID)data.dwID)
            {
                case SIMCONNECT_RECV_ID.EVENT_FILENAME:

                    break;
                case SIMCONNECT_RECV_ID.QUIT:
                    logger.LogInformation("Quit");
                    break;
            }

            switch ((EVENTS)data.uEventID)
            {
                case EVENTS.SIM_START:
                    logger.LogInformation("Sim start");
                    break;
                case EVENTS.SIM_STOP:
                    logger.LogInformation("Sim stop");
                    break;
                case EVENTS.PAUSED:
                    logger.LogInformation("Paused");
                    //simconnect.TransmitClientEvent((uint)SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SEND_UNPAUSE, (uint)0, GROUPID.FLAG, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    break;
                case EVENTS.FLIGHTPLAN_ACTIVATED:
                    logger.LogInformation("Flight plan activated");

                    break;
                case EVENTS.FLIGHTPLAN_DEACTIVATED:
                    logger.LogInformation("Flight plan deactivated");
                    break;
                case EVENTS.CRASHED:
                    logger.LogInformation("Crashed");
                    Crashed?.Invoke(this, new EventArgs());
                    break;
                case EVENTS.CRASH_RESET:
                    logger.LogInformation("Crash reset");
                    CrashReset?.Invoke(this, new EventArgs());
                    break;
                case EVENTS.POSITION_CHANGED:
                    logger.LogInformation("Position changed");
                    break;
                case EVENTS.SCREENSHOT:
                    logger.LogInformation("Screenshot");

                    break;
            }
        }

        private void Simconnect_OnRecvEventFilename(SimConnect sender, SIMCONNECT_RECV_EVENT_FILENAME data)
        {
            logger.LogInformation("OnRecvEventFilename dwID " + data.dwID + " uEventID " + data.uEventID);
            switch ((EVENTS)data.uEventID)
            {
                case EVENTS.AIRCRAFT_LOADED:
                    logger.LogInformation("Aircraft loaded");
                    simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.AIRCRAFT_DATA, DEFINITIONS.PlaneData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                    break;
                case EVENTS.FLIGHT_LOADED:
                    {
                        logger.LogInformation("Flight loaded");
                        //var evt = (SIMCONNECT_RECV_EVENT_FILENAME)data;

                    }
                    break;
            }
        }

        void simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger.LogInformation("Connected to Prepar3D");

            //simconnect.RequestFlightSegmentCount(DATA_REQUESTS.FLIGHT_PLAN);
            simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.AIRCRAFT_DATA, DEFINITIONS.PlaneData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
            simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.FLIGHT_PLAN_STATUS, DEFINITIONS.FlightPlan, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
            simconnect.RequestDataOnSimObject(DATA_REQUESTS.ENVIRONMENT_DATA, DEFINITIONS.EnvironmentData, 0, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            simconnect.SubscribeToFacilities(SIMCONNECT_FACILITY_LIST_TYPE.AIRPORT, DATA_REQUESTS.SUBSCRIBE_GENERIC);

            simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.FLIGHT_STATUS, DEFINITIONS.FlightStatus, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
        }

        // The case where the user closes Prepar3D
        void simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            logger.LogInformation("Prepar3D has exited");
            Closed?.Invoke(this, new EventArgs());
            CloseConnection();
        }

        void simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            logger.LogError("Exception received: {0}", data.dwException);
        }

        private void simconnect_OnRecvAirportList(SimConnect sender, SIMCONNECT_RECV_AIRPORT_LIST data)
        {
            logger.LogDebug("Received Airport List");

            AirportListUpdated?.Invoke(this, new AirportListUpdatedEventArgs(data.rgData.Cast<SIMCONNECT_DATA_FACILITY_AIRPORT>().Select(airport => new AirportData
            {
                Icao = airport.Icao,
                Latitude = airport.Latitude,
                Longitude = airport.Longitude,
                Altitude = airport.Altitude
            })));
        }

        private void RecoverFromError()
        {
            string errorMessage;
            //Disconnect();

            //bool wasSuccess = Connect(out errorMessage);

            //// Start monitoring the user's SimObject. This will continuously monitor information
            //// about the user's Stations attached to their SimObject.
            //if (wasSuccess)
            //{
            //    StartMonitoring();
            //}
        }
    }
}
