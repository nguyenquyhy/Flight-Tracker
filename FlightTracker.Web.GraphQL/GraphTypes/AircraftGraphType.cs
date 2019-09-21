using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using GraphQL.Types;
using System.Linq;

namespace FlightTracker.Web
{
    public class AircraftGraphType : ObjectGraphType<AircraftData>
    {
        public AircraftGraphType(IFlightStorage flightStorage)
        {
            Field(o => o.Title);
            Field(o => o.Type);
            Field(o => o.Model);
            Field(o => o.TailNumber);
            Field(o => o.Airline);
            Field(o => o.FlightNumber);

            FieldAsync<ListGraphType<StringGraphType>>("pictureUrls",
                arguments: new QueryArguments
                {
                    new QueryArgument(typeof(IntGraphType)) { Name = "limit" }
                },
                resolve: async context =>
                {
                    var list = await flightStorage.GetAircraftPictureUrlsAsync(context.Source.TailNumber);
                    if (context.Arguments.TryGetValue("limit", out var limitObj) && limitObj is int limit)
                    {
                        list = list.TakeLast(limit).ToList();
                    }
                    return list;
                });
    }
}
}
