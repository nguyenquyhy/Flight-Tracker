using FlightTracker.DTOs;
using GraphQL.Types;

namespace FlightTracker.Web
{
    public class FlightPlanInput : InputObjectGraphType<FlightPlan>
    {
        public FlightPlanInput()
        {
            Field(o => o.Title);
            Field(o => o.Departure, type: typeof(PointInput));
            Field(o => o.Destination, type: typeof(PointInput));
            Field(o => o.CruisingAltitude);
            Field(o => o.Waypoints, type: typeof(ListGraphType<WaypointInput>));
        }
    }

    public class PointInput: InputObjectGraphType<Point>
    {
        public PointInput()
        {
            Field(o => o.Id);
            Field(o => o.Name);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.Position, nullable: true);
        }
    }

    public class WaypointInput : InputObjectGraphType<Waypoint>
    {
        public WaypointInput()
        {
            Field(o => o.Id);
            Field(o => o.Latitude);
            Field(o => o.Longitude);
            Field(o => o.Type);
            Field(o => o.Airway, nullable: true);
        }
    }
}
