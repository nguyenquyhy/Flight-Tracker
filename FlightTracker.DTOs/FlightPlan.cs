using System.Collections.Generic;

namespace FlightTracker.DTOs
{

    public class FlightPlan
    {
        public string Title { get; set; }
        public Point Departure { get; set; }
        public Point Destination { get; set; }
        public double CruisingAltitude { get; set; }
        public List<Waypoint> Waypoints { get; set; }
    }

    public class Waypoint
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string LLA { get; set; }
        public string Airway { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class Point : Waypoint
    {
        public string Name { get; set; }
        public string Position { get; set; }
    }
}
