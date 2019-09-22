using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class FlightStatusInput : InputObjectGraphType<FlightStatus>
    {
        public FlightStatusInput()
        {
            Field(o => o.SimTime);
            Field(o => o.LocalTime, nullable: true);
            Field(o => o.ZuluTime, nullable: true);
            Field(o => o.AbsoluteTime, nullable: true);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.Altitude);
            Field(o => o.AltitudeAboveGround);

            Field(o => o.Heading);
            Field(o => o.TrueHeading);
            
            Field(o => o.GroundSpeed);
            Field(o => o.IndicatedAirSpeed);
            Field(o => o.VerticalSpeed);

            Field(o => o.FuelTotalQuantity);

            Field(o => o.Pitch);
            Field(o => o.Bank);

            Field(o => o.IsOnGround);
            Field(o => o.StallWarning);
            Field(o => o.OverspeedWarning);

            Field(o => o.IsAutopilotOn);

            Field(o => o.ScreenshotUrl, nullable: true);
        }
    }
}
