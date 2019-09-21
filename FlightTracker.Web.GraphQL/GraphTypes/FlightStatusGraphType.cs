using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class FlightStatusGraphType : ObjectGraphType<FlightStatus>
    {
        public FlightStatusGraphType()
        {
            Field(o => o.SimTime);
            Field(o => o.FuelTotalQuantity);
            Field(o => o.IndicatedAirSpeed);
            Field(o => o.VerticalSpeed);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.ScreenshotUrl, nullable: true);
            Field(o => o.IsOnGround);
            Field(o => o.IsAutopilotOn);
        }
    }
}
