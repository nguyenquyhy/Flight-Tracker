using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using GraphQL.Types;
using System.Collections.Generic;
using System.Linq;

namespace FlightTracker.Web
{
    public class RootQueryGraphType : ObjectGraphType
    {
        public RootQueryGraphType(IFlightStorage flightStorage)
        {
            FieldAsync<ListGraphType<FlightGraphType>>(
                "flights",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(IntGraphType)) { Name = "last" }
                ),
                resolve: async context =>
                {
                    var data = await flightStorage.GetFlightsAsync();

                    if (context.Arguments.TryGetValue<int>("last", out var last))
                    {
                        data = data.OrderByDescending(a => a.AddedDateTime).Take(last);
                    }

                    return data;
                }
                );

            FieldAsync<FlightGraphType>(
                 "flight",
                 arguments: new QueryArguments(
                     new QueryArgument(typeof(NonNullGraphType<StringGraphType>)) { Name = "id" }
                 ),
                 resolve: async context =>
                 {
                     var id = context.GetArgument<string>("id");
                     return await flightStorage.GetFlightAsync(id);
                 }
                 );

            FieldAsync<ListGraphType<AircraftGraphType>>(
                "aircrafts",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
                    var result = new List<AircraftData>();
                    await foreach (var aircraft in flightStorage.GetAircraftsAsync())
                    {
                        result.Add(aircraft);
                    }
                    return result;
                }
                );

            FieldAsync<AircraftGraphType>(
                "aircraft",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(NonNullGraphType<StringGraphType>)) { Name = "tailNumber" }
                ),
                resolve: async context =>
                {
                    var tailNumber = context.GetArgument<string>("tailNumber");
                    return await flightStorage.GetAircraftAsync(tailNumber);
                }
                );
        }
    }
}
