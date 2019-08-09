using System;

namespace FlightTracker.Web.Data
{
    public class FlightModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public DateTimeOffset SimStartDateTime { get; set; }
        public DateTimeOffset SimEndDateTime { get; set; }

        public string Airline { get; set; }
        public string FlightNumber { get; set; }
        public string AirportFrom { get; set; }
        public string AirportTo { get; set; }
        
        public AircraftModel Aircraft { get; set; }

        public int FuelUsed { get; set; }
        public int DistanceFlown { get; set; }

        public int TakeOffIAS { get; set; }
        public int LandingIAS { get; set; }
        public int LandingVS { get; set; }

        public string State { get; set; }

        // Flight plan

        // Flight path
    }
}
