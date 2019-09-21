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
                    var data = await flightStorage.GetAllAsync();

                    if (context.Arguments.TryGetValue("last", out var lastObj) && lastObj is int last)
                    {
                        data = data.OrderByDescending(a => a.AddedDateTime).Take(last);
                    }

                    return data;
                }
                );

            FieldAsync<FlightGraphType>(
                 "flight",
                 arguments: new QueryArguments(
                     new QueryArgument(typeof(StringGraphType)) { Name = "id" }
                 ),
                 resolve: async context =>
                 {
                     if (context.Arguments.TryGetValue("id", out var idObj) && idObj is string id)
                     {
                         return await flightStorage.GetAsync(id);
                     }
                     return null;
                 }
                 );

            FieldAsync<ListGraphType<AircraftGraphType>>(
                "aircrafts",
                arguments: new QueryArguments(),
                resolve: async context =>
                {
                    var result = new List<AircraftData>();
                    await foreach (var aircraft in flightStorage.GetAllAircraftsAsync())
                    {
                        result.Add(aircraft);
                    }
                    return result;
                }
                );

            FieldAsync<AircraftGraphType>(
                "aircraft",
                arguments: new QueryArguments(
                    new QueryArgument(typeof(StringGraphType)) { Name = "tailNumber" }
                ),
                resolve: async context =>
                {
                    if (context.Arguments.TryGetValue("tailNumber", out var tailNumberObj) && tailNumberObj is string tailNumber)
                    {
                        return await flightStorage.GetAircraftAsync(tailNumber);
                    }
                    return null;
                }
                );
        }
    }
}
