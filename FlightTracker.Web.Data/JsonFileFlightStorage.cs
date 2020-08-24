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
        private readonly IIdProvider idProvider;

        public JsonFileFlightStorage(IIdProvider idProvider, string filePath) : base(filePath)
        {
            this.idProvider = idProvider;
        }

        public async Task<IEnumerable<FlightData>> GetFlightsAsync()
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            return flights.Select(o => o.Value);
        }

        public async Task<FlightData> GetFlightAsync(string id)
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            return flights[id];
        }

        public async Task<FlightData> AddFlightAsync(FlightData data)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = new FlightWrapper(data)
                {
                    Id = await idProvider.GenerateAsync().ConfigureAwait(false),
                    AddedDateTime = DateTimeOffset.UtcNow
                };
                flights.Add(flight.Id, flight);
                await SaveAsync(flights).ConfigureAwait(false);
                return flight.ToDTO();
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<FlightData> InsertOrUpdateFlightAsync(string id, FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                if (flights.TryGetValue(id, out var flight))
                {
                    flight.CopyFrom(data);
                }
                else
                {
                    flight = new FlightWrapper(data)
                    {
                        Id = id,
                        AddedDateTime = DateTimeOffset.UtcNow
                    };
                    flights.Add(id, flight);
                }
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

                var flight = flights[id];
                if (data.Title != null) flight.Title = data.Title;
                if (data.Description != null) flight.Description = data.Description;
                if (data.Aircraft != null) flight.Aircraft = data.Aircraft;
                if (data.Airline != null) flight.Airline = data.Airline;
                if (data.FlightNumber != null) flight.FlightNumber = data.FlightNumber;
                if (data.VideoUrl != null) flight.VideoUrl = data.VideoUrl;

                await SaveAsync(flights).ConfigureAwait(false);

                return flight;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<bool> DeleteFlightAsync(string id)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                if (flights.Remove(id))
                {
                    await SaveAsync(flights).ConfigureAwait(false);
                    return true;
                }
                return false;
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
                var flight = flights[id];

                return flight.Statuses;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task UpdateRouteAsync(string id, List<FlightStatus> route)
        {
            try
            {
                await flightsSm.WaitAsync().ConfigureAwait(false);

                var flights = await LoadAsync().ConfigureAwait(false);
                var flight = flights[id];
                flight.Statuses = route;
                await SaveAsync(flights).ConfigureAwait(false);
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async IAsyncEnumerable<AircraftData> GetAircraftsAsync()
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            var groups = flights
                .Select(o => o.Value)
                .Where(o => o.Aircraft != null)
                .GroupBy(o => o.Aircraft.TailNumber);

            foreach (var group in groups)
            {
                var aircraft = group.First().Aircraft;
                yield return aircraft;
            }
        }

        public async Task<AircraftData> GetAircraftAsync(string tailNumber)
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            var flightsWithAircraft = flights
                .Select(o => o.Value)
                .Where(o => tailNumber.Equals(o.Aircraft?.TailNumber, StringComparison.InvariantCultureIgnoreCase));
            var aircraft = flightsWithAircraft.First().Aircraft;
            return aircraft;
        }

        public async Task<List<string>> GetAircraftPictureUrlsAsync(string tailNumber)
        {
            var flights = await LoadAsync().ConfigureAwait(false);
            return flights
                .Select(o => o.Value)
                .Where(o => tailNumber.Equals(o.Aircraft?.TailNumber, StringComparison.InvariantCultureIgnoreCase))
                .Where(o => o.Statuses != null)
                .SelectMany(o => o.Statuses.Where(s => !string.IsNullOrEmpty(s.ScreenshotUrl)))
                .Select(o => o.ScreenshotUrl).ToList();
        }
    }

    public abstract class JsonFileFlightStorageBase
    {
        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);
        private readonly string filePath;
        private Dictionary<string, FlightWrapper> data = null;

        public JsonFileFlightStorageBase(string filePath)
        {
            this.filePath = filePath;
        }

        protected async Task<Dictionary<string, FlightWrapper>> LoadAsync()
        {
            if (data == null)
            {
                try
                {
                    await sm.WaitAsync().ConfigureAwait(false);
                    if (File.Exists(filePath))
                    {
                        var dataString = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                        var list = JsonConvert.DeserializeObject<List<FlightWrapper>>(dataString);
                        data = list.ToDictionary(o => o.Id, o => o);
                    }
                    else
                    {
                        data = new Dictionary<string, FlightWrapper>();
                    }
                }
                finally
                {
                    sm.Release();
                }
            }
            return data;
        }

        protected async Task SaveAsync(Dictionary<string, FlightWrapper> data)
        {
            if (data == null) return;

            try
            {
                await sm.WaitAsync().ConfigureAwait(false);
                if (File.Exists(filePath))
                {
                    if (File.Exists(filePath + ".bak")) File.Delete(filePath + ".bak");
                    File.Move(filePath, filePath + ".bak");
                }
                var list = data.Select(o => o.Value);
                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(list, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })).ConfigureAwait(false);

                this.data = data;
            }
            finally
            {
                sm.Release();
            }
        }

    }
}
