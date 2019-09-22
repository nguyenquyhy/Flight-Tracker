using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class PatchFlightInput : InputObjectGraphType<FlightData>
    {
        public PatchFlightInput()
        {
            Field(o => o.Title, nullable: true);
            Field(o => o.Description, nullable: true);

            Field(o => o.EndDateTime, nullable: true);

            Field(o => o.Airline, nullable: true);
            Field(o => o.FlightNumber, nullable: true);

            Field(o => o.StatusTakeOff, nullable: true, type: typeof(FlightStatusInput));
            Field(o => o.StatusLanding, nullable: true, type: typeof(FlightStatusInput));

            Field(o => o.TakeOffDateTime, nullable: true);
            Field(o => o.LandingDateTime, nullable: true);

            Field(o => o.AirportFrom, nullable: true);
            Field(o => o.AirportTo, nullable: true);

            Field(o => o.State, type: typeof(FlightStateGraphType));

            Field(o => o.FlightPlan, type: typeof(FlightPlanInput));
        }
    }
}
