using System;

namespace FlightTracker.Web.Data
{
    public class AircraftModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Model { get; set; }
        public string TailNumber { get; set; }
        public string Airline { get; set; }

        public string PictureUrl { get; set; }
    }
}
