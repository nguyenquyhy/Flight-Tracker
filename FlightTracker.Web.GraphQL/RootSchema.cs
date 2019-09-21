using GraphQL.Types;
using System;

namespace FlightTracker.Web
{
    public class RootSchema : Schema
    {
        public RootSchema(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Query = serviceProvider.GetService(typeof(RootQueryGraphType)) as RootQueryGraphType;
        }
    }
}
