using FlightTracker.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public class JsonFileFlightStorage : IFlightStorage
    {
        private readonly string filePath;
        private List<FlightData> data = null;

        public JsonFileFlightStorage(string filePath)
        {
            this.filePath = filePath;
        }

        public async Task<IEnumerable<FlightData>> GetAllAsync()
        {
            return await LoadAsync();
        }

        public async Task<FlightData> GetAsync(string id)
        {
            var flights = await LoadAsync();
            return flights.FirstOrDefault(flight => flight.Id == id);
        }

        private readonly SemaphoreSlim flightsSm = new SemaphoreSlim(1);

        public async Task<FlightData> AddAsync(FlightData data)
        {
            try
            {
                await flightsSm.WaitAsync();

                var flights = await LoadAsync();
                data.Id = Guid.NewGuid().ToString("N");
                data.AddedDateTime = DateTimeOffset.UtcNow;
                flights.Add(data);
                await SaveAsync(flights);
                return data;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<FlightData> UpdateAsync(string id, FlightData data)
        {
            try
            {
                await flightsSm.WaitAsync();

                var flights = await LoadAsync();
                flights.RemoveAll(flight => flight.Id == id);
                flights.Add(data);
                await SaveAsync(flights);

                return data;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        public async Task<FlightData> PatchAsync(string id, FlightData data)
        {
            try
            {
                await flightsSm.WaitAsync();

                var flights = await LoadAsync();

                var flight = flights.First(o => o.Id == id);
                if (data.Title != null) flight.Title = data.Title;
                if (data.Description != null) flight.Description = data.Description;

                await SaveAsync(flights);

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
                await flightsSm.WaitAsync();

                var flights = await LoadAsync();
                var count = flights.RemoveAll(flight => flight.Id == id);
                await SaveAsync(flights);

                return count > 0;
            }
            finally
            {
                flightsSm.Release();
            }
        }

        private static readonly SemaphoreSlim sm = new SemaphoreSlim(1);

        private async Task<List<FlightData>> LoadAsync()
        {
            if (data == null)
            {
                try
                {
                    await sm.WaitAsync();
                    if (File.Exists(filePath))
                    {
                        var dataString = await File.ReadAllTextAsync(filePath);
                        data = JsonConvert.DeserializeObject<List<FlightData>>(dataString);
                    }
                    else
                    {
                        data = new List<FlightData>();
                    }
                }
                finally
                {
                    sm.Release();
                }
            }
            return data;
        }

        private async Task SaveAsync(List<FlightData> data)
        {
            if (data == null) return;

            try
            {
                await sm.WaitAsync();
                await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
            }
            finally
            {
                sm.Release();
            }
        }
    }
}
