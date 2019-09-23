using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using GraphQL.Authorization;
using GraphQL.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Web
{
    public class RootMutationGraphType : ObjectGraphType
    {
        private readonly IFlightStorage flightStorage;

        public RootMutationGraphType(IFlightStorage flightStorage)
        {
            this.flightStorage = flightStorage;

            FieldAsync<FlightGraphType>("addFlight",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddFlightInput>> { Name = "flight" }
                ),
                resolve: ResolveAddFlight);

            FieldAsync<FlightGraphType>("updateFlight",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<UpdateFlightInput>> { Name = "flight" }
                ),
                resolve: ResolveUpdateFlight);

            FieldAsync<FlightGraphType>("patchFlight",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<PatchFlightInput>> { Name = "flight" }
                ),
                resolve: ResolvePatchFlight);

            FieldAsync<FlightGraphType>("appendRoute",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" },
                    new QueryArgument<NonNullGraphType<ListGraphType<FlightStatusInput>>> { Name = "route" }
                ),
                resolve: ResolveAppendRoute);

            FieldAsync<BooleanGraphType>("deleteFlight",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "id" }
                ),
                resolve: ResolveDeleteFlight).AuthorizeWith("Authenticated");
        }

        private async Task<object> ResolveAddFlight(ResolveFieldContext<object> context)
        {
            var flightInput = context.GetArgument<FlightData>("flight");
            return await flightStorage.AddAsync(flightInput);
        }

        private async Task<object> ResolveUpdateFlight(ResolveFieldContext<object> context)
        {
            var id = context.GetArgument<string>("id");
            var flightInput = context.GetArgument<FlightData>("flight");
            return await flightStorage.UpdateAsync(id, flightInput);
        }

        private async Task<object> ResolvePatchFlight(ResolveFieldContext<object> context)
        {
            var id = context.GetArgument<string>("id");
            var flightInput = context.GetArgument<FlightData>("flight");
            return await flightStorage.PatchAsync(id, flightInput);
        }

        private async Task<object> ResolveAppendRoute(ResolveFieldContext<object> context)
        {
            var id = context.GetArgument<string>("id");
            var route = (await flightStorage.GetRouteAsync(id))?.ToList();

            if (route != null)
            {
                var routeInput = context.GetArgument<List<FlightStatus>>("route");
                if (route.Count == 0)
                {
                    route = routeInput;
                }
                else
                {
                    var lastTime = route.Last().SimTime;
                    route.AddRange(routeInput.Where(o => o.SimTime > lastTime));
                }
                await flightStorage.UpdateRouteAsync(id, route);
                return await flightStorage.GetAsync(id);
            }
            return null;
        }

        private async Task<object> ResolveDeleteFlight(ResolveFieldContext<object> context)
        {
            var id = context.GetArgument<string>("id");
            return await flightStorage.DeleteAsync(id);
        }
    }
}
