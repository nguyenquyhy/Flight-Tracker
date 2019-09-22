using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class AddFlightInput : InputObjectGraphType<InputFlightData>
    {
        public AddFlightInput()
        {
            Field(o => o.Title, nullable: true);
            Field(o => o.Description, nullable: true);
            Field(o => o.StartDateTime);
            Field(o => o.EndDateTime, nullable: true);
            Field(o => o.TakeOffDateTime, nullable: true);
            Field(o => o.LandingDateTime, nullable: true);

            Field(o => o.AirportFrom, nullable: true);
            Field(o => o.AirportTo, nullable: true);

            Field(o => o.Aircraft, type: typeof(NonNullGraphType<AircraftInput>));

            Field(o => o.StatusTakeOff, nullable: true, type: typeof(FlightStatusInput));
            Field(o => o.StatusLanding, nullable: true, type: typeof(FlightStatusInput));

            Field(o => o.State, type: typeof(NonNullGraphType<FlightStateGraphType>));

            Field(o => o.FlightPlan, type: typeof(FlightPlanInput));
        }
    }
}
