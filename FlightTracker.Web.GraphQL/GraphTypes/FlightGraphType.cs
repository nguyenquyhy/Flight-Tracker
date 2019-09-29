using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using GraphQL.Types;
using System.Linq;

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

            Field(o => o.TakeOffDateTime, nullable: true);
            Field(o => o.LandingDateTime, nullable: true);

            Field(o => o.AirportFrom, nullable: true);
            Field(o => o.AirportTo, nullable: true);

            Field(o => o.State, type: typeof(FlightStateGraphType));

            Field(o => o.FlightPlan, type: typeof(FlightPlanGraphType));

            FieldAsync<ListGraphType<FlightStatusGraphType>>("route",
                arguments: new QueryArguments(
                    new QueryArgument<UIntGraphType> { Name = "last" }
                ),
                resolve: async context =>
                {
                    System.Diagnostics.Debug.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] Start getting route of {context.Source.Id}...");
                    var route = await flightStorage.GetRouteAsync(context.Source.Id);
                    System.Diagnostics.Debug.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] Got route of {context.Source.Id}.");
                    if (context.Arguments.TryGetValue<uint>("last", out var last))
                    {
                        route = route.TakeLast((int)last).ToList();
                        System.Diagnostics.Debug.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] Filtered route of {context.Source.Id}.");
                    }
                    return route;
                });
        }
    }

    public class FlightStateGraphType : EnumerationGraphType<FlightState>
    {

    }
}
