using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class AircraftInput : InputObjectGraphType<AircraftData>
    {
        public AircraftInput()
        {
            Field(o => o.Title);
            Field(o => o.Type);
            Field(o => o.Model);
            Field(o => o.TailNumber);
            Field(o => o.Airline);
            Field(o => o.FlightNumber);
        }
    }
}
