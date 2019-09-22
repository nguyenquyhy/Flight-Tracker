using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class FlightGraphType : ObjectGraphType<FlightData>
    {
        public FlightGraphType(IFlightStorage flightStorage)
        {
            Field(o => o.Id);
            Field(o => o.Title, nullable: true);
            Field(o => o.Description, nullable: true);

            Field(o => o.AddedDateTime);
            Field(o => o.StartDateTime);
            Field(o => o.EndDateTime, nullable: true);

            Field(o => o.FlightNumber, nullable: true);

            Field(o => o.Aircraft, type: typeof(AircraftGraphType));

            Field(o => o.StatusTakeOff, nullable: true, type: typeof(FlightStatusGraphType));
            Field(o => o.StatusLanding, nullable: true, type: typeof(FlightStatusGraphType));

            Field(o => o.TakeOffDateTime);
            Field(o => o.TakeOffAbsoluteTime);
            Field(o => o.TakeOffLocalTime);
            Field(o => o.TakeOffZuluTime);

            Field<IntGraphType>("landingLocalTime", resolve: context => context.Source.StatusTakeOff != null && context.Source.StatusLanding != null ? 
                (int?)(int)(context.Source.TakeOffLocalTime - context.Source.StatusTakeOff.SimTime + context.Source.StatusLanding.SimTime) : null);

            Field(o => o.LandingDateTime, nullable: true);

            Field(o => o.AirportFrom, nullable: true);
            Field(o => o.AirportTo, nullable: true);

            Field(o => o.State, type: typeof(FlightStateGraphType));

            FieldAsync<ListGraphType<FlightStatusGraphType>>("route",
                resolve: async context =>
                {
                    return await flightStorage.GetRouteAsync(context.Source.Id);
                });
        }
    }

    public class FlightStateGraphType : EnumerationGraphType<FlightState>
    {

    }
}
