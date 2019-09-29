using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class FlightPlanGraphType : ObjectGraphType<FlightPlan>
    {
        public FlightPlanGraphType()
        {
            Field(o => o.Title);
            Field(o => o.Departure, type: typeof(PointGraphType));
            Field(o => o.Destination, type: typeof(PointGraphType));
            Field(o => o.CruisingAltitude);
            Field(o => o.Waypoints, type: typeof(ListGraphType<WaypointGraphType>));
        }
    }

    public class PointGraphType : ObjectGraphType<Point>
    {
        public PointGraphType()
        {
            Field(o => o.Id);
            Field(o => o.Name);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.Position, nullable: true);
        }
    }

    public class WaypointGraphType : ObjectGraphType<Waypoint>
    {
        public WaypointGraphType()
        {
            Field(o => o.Id);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.Type);
            Field(o => o.Airway, nullable: true);
        }
    }
}
