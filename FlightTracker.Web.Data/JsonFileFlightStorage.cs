using FlightTracker.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public class JsonFileFlightStorage : JsonFileFlightStorageBase, IFlightStorage
    {
        private static readonly SemaphoreSlim flightsSm = new SemaphoreSlim(1);

        public JsonFileFlightStorage(string filePath) : base(filePath)
        {
            
        }

        public async Task<IEnumerable<FlightData>> GetAllAsync()
        {
            return await LoadAsync().ConfigureAwait(false);
        }

        public async Task<FlightData> GetAsync(string id)
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            return flights.FirstOrDefault(flight => flight.Id == id);
        }

        public async Task<FlightData> AddAsync(FlightData data)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = new FlightWrapper(data)
                {
                    Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                    AddedDateTime = DateTimeOffset.UtcNow
                };
                flights.Add(flight);
                await SaveAsync(flights).ConfigureAwait(false);
                return flight.ToDTO();
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<FlightData> UpdateAsync(string id, FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = flights.First(o => o.Id == id);
                flight.Update(data);
                await SaveAsync(flights).ConfigureAwait(false);

                return flight.ToDTO();
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<FlightData> PatchAsync(string id, FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);

                var flight = flights.First(o => o.Id == id);
                if (data.Title != null) flight.Title = data.Title;
                if (data.Description != null) flight.Description = data.Description;

                await SaveAsync(flights).ConfigureAwait(false);

                return flight;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var count = flights.RemoveAll(flight => flight.Id == id);
                await SaveAsync(flights).ConfigureAwait(false);

                return count > 0;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<IEnumerable<FlightStatus>> GetRouteAsync(string id)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = flights.First(o => o.Id == id);

                return flight.Statuses;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<IEnumerable<FlightStatus>> UpdateRouteAsync(string id, List<FlightStatus> route)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = flights.First(o => o.Id == id);
                flight.Statuses = route;
                await SaveAsync(flights).ConfigureAwait(false);

                return flight.Statuses;
            }
            finally
            {
                flightsSm.Release();
            }
        }
    }

    public abstract class JsonFileFlightStorageBase
    {
        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);
        private readonly string filePath;
        private List<FlightWrapper> data = null;


        public JsonFileFlightStorageBase(string filePath)
        {
            this.filePath = filePath;
        }

        protected async Task<List<FlightWrapper>> LoadAsync()
        {
            if (data == null)
            {
                try
                {
                    await sm.WaitAsync().ConfigureAwait(false);
                    if (File.Exists(filePath))
                    {
                        var dataString = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                        data = JsonConvert.DeserializeObject<List<FlightWrapper>>(dataString);
                    }
                    else
                    {
                        data = new List<FlightWrapper>();
                    }
                }
                finally
                {
                    sm.Release();
                }
            }
            return data;
        }

        protected async Task SaveAsync(List<FlightWrapper> data)
        {
            if (data == null) return;

            try
            {
                await sm.WaitAsync().ConfigureAwait(false);
                if (File.Exists(filePath)) File.Move(filePath, filePath + ".bak", true);
                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })).ConfigureAwait(false);
            }
            finally
            {
                sm.Release();
            }
        }

    }

    public class FlightWrapper : FlightData
    {
        public FlightWrapper() { }
        public FlightWrapper(FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            Id = data.Id;
            Title = data.Title;
            Description = data.Description;

            AddedDateTime = data.AddedDateTime;

            StartDateTime = data.StartDateTime;
            EndDateTime = data.EndDateTime;

            TakeOffDateTime = data.TakeOffDateTime;
            TakeOffLocalTime = data.TakeOffLocalTime;
            TakeOffZuluTime = data.TakeOffZuluTime;
            TakeOffAbsoluteTime = data.TakeOffAbsoluteTime;
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
                TakeOffLocalTime = TakeOffLocalTime,
                TakeOffZuluTime = TakeOffZuluTime,
                TakeOffAbsoluteTime = TakeOffAbsoluteTime,
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

        internal void Update(FlightData data)
        {
            if (Id != data.Id) throw new InvalidOperationException($"Cannot update Id!");
            Title = data.Title;
            Description = data.Description;

            AddedDateTime = data.AddedDateTime;

            StartDateTime = data.StartDateTime;
            EndDateTime = data.EndDateTime;

            TakeOffDateTime = data.TakeOffDateTime;
            TakeOffLocalTime = data.TakeOffLocalTime;
            TakeOffZuluTime = data.TakeOffZuluTime;
            TakeOffAbsoluteTime = data.TakeOffAbsoluteTime;
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
        }
    }
}
