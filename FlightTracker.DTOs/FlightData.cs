using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace FlightTracker.DTOs
{
    [System.Diagnostics.DebuggerDisplay("{Title}")]
    public class InputFlightData
    {
        public InputFlightData() { }

        public InputFlightData(InputFlightData data)
        {
            var properties = this.GetType().GetProperties();
            foreach (var property in properties)
            {
                property.SetValue(this, property.GetValue(data));
            }
        }

        public string Title { get; set; }
        public string Description { get; set; }

        public DateTimeOffset StartDateTime { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset? EndDateTime { get; set; }

        public DateTimeOffset? TakeOffDateTime { get; set; }
        public DateTimeOffset? LandingDateTime { get; set; }

        public string Airline { get; set; }
        public string FlightNumber { get; set; }
        public string AirportFrom { get; set; }
        public string AirportTo { get; set; }

        public AircraftData Aircraft { get; set; }

        public FlightStatus StatusTakeOff { get; set; }
        public FlightStatus StatusLanding { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FlightState State { get; set; } = FlightState.Started;

        public FlightPlan FlightPlan { get; set; }

        public string VideoUrl { get; set; }
    }

    public class FlightData : InputFlightData
    {
        public string Id { get; set; }

        public DateTimeOffset AddedDateTime { get; set; }

        public int? FuelUsed { get; set; }
        public int? DistanceFlown { get; set; }

        public void CopyFrom(FlightData data)
        {
            if (Id != data.Id) throw new InvalidOperationException($"Cannot update Id!");
            Title = data.Title;
            Description = data.Description;

            AddedDateTime = data.AddedDateTime;

            StartDateTime = data.StartDateTime;
            EndDateTime = data.EndDateTime;

            TakeOffDateTime = data.TakeOffDateTime;
            LandingDateTime = data.LandingDateTime;

            Airline = data.Airline;
            FlightNumber = data.FlightNumber;
            AirportFrom = data.AirportFrom;
            AirportTo = data.AirportTo;

            Aircraft = data.Aircraft;

            FuelUsed = data.FuelUsed;
            DistanceFlown = data.DistanceFlown;

            StatusTakeOff = data.StatusTakeOff;
            StatusLanding = data.StatusLanding;

            State = data.State;

            FlightPlan = data.FlightPlan;

            VideoUrl = data.VideoUrl;
        }
    }

    public enum FlightState
    {
        Started,
        Enroute,
        Arrived,
        Crashed,
        Lost
    }
}
