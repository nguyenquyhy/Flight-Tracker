using FlightTracker.DTOs;
using System;

namespace FlightTracker.Web.Data.Sqlite
{
    public class FlightStatusWrapper
    {
        public string FlightId { get; set; }


        public double SimTime { get; set; }
        public int? LocalTime { get; set; }
        public int? ZuluTime { get; set; }
        public long? AbsoluteTime { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double AltitudeAboveGround { get; set; }

        public double Heading { get; set; }
        public double TrueHeading { get; set; }

        public double GroundSpeed { get; set; }
        public double IndicatedAirSpeed { get; set; }
        public double VerticalSpeed { get; set; }

        public double FuelTotalQuantity { get; set; }

        public double Pitch { get; set; }
        public double Bank { get; set; }

        public bool IsOnGround { get; set; }
        public bool StallWarning { get; set; }
        public bool OverspeedWarning { get; set; }

        public bool IsAutopilotOn { get; set; }

        public string ScreenshotUrl { get; set; }

        public FlightStatusWrapper()
        {

        }

        public FlightStatusWrapper(string flightId, FlightStatus status)
        {
            if (string.IsNullOrWhiteSpace(flightId)) throw new ArgumentNullException(nameof(flightId));
            if (status == null) throw new ArgumentNullException(nameof(status));

            FlightId = flightId;

            foreach (var field in status.GetType().GetProperties())
            {
                var thisField = this.GetType().GetProperty(field.Name);

                if (thisField != null)
                {
                    thisField.SetValue(this, field.GetValue(status));
                }
            }
        }

        internal FlightStatus ToDTO()
        {
            var status = new FlightStatus();
            foreach (var field in status.GetType().GetProperties())
            {
                var thisField = this.GetType().GetProperty(field.Name);

                if (thisField != null)
                {
                    field.SetValue(status, thisField.GetValue(this));
                }
            }
            return status;
        }
    }
}
