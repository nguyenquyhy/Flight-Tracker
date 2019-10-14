using FlightTracker.DTOs;
using System;
using System.Collections.Generic;

namespace FlightTracker.Web.Data
{
    public class FlightWrapper : FlightData
    {
        public FlightWrapper() { }
        public FlightWrapper(FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Id = data.Id;
            CopyFrom(data);
        }

        public List<FlightStatus> Statuses { get; set; } = new List<FlightStatus>();

        public FlightData ToDTO()
        {
            return new FlightData
            {
                Id = Id,
                Title = Title,
                Description = Description,

                AddedDateTime = AddedDateTime,

                StartDateTime = StartDateTime,
                EndDateTime = EndDateTime,

                TakeOffDateTime = TakeOffDateTime,
                LandingDateTime = LandingDateTime,

                Airline = Airline,
                FlightNumber = FlightNumber,
                AirportFrom = AirportFrom,
                AirportTo = AirportTo,

                Aircraft = Aircraft,

                FuelUsed = FuelUsed,
                DistanceFlown = DistanceFlown,

                StatusTakeOff = StatusTakeOff,
                StatusLanding = StatusLanding,

                State = State
            };
        }
    }
}
