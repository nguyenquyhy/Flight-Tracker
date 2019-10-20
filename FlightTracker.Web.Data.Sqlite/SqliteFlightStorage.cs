using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlightTracker.DTOs;
using FlightTracker.Web.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlightTracker.Web.Data
{
    public class SqliteFlightStorage : IFlightStorage
    {
        private readonly SqliteDbContext dbContext;
        private readonly IIdProvider idProvider;
        private readonly ILogger<SqliteFlightStorage> logger;

        public SqliteFlightStorage(SqliteDbContext dbContext, IIdProvider idProvider, ILogger<SqliteFlightStorage> logger)
        {
            this.dbContext = dbContext;
            this.idProvider = idProvider;
            this.logger = logger;
        }

        public Task<IEnumerable<FlightData>> GetFlightsAsync()
        {
            return Task.FromResult(dbContext.Flights.AsEnumerable());
        }

        public async Task<FlightData> GetFlightAsync(string id)
        {
            return await dbContext.Flights.FindAsync(id).ConfigureAwait(false);
        }

        public async Task<FlightData> AddFlightAsync(FlightData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            data.Id = await idProvider.GenerateAsync().ConfigureAwait(false);
            data.AddedDateTime = DateTimeOffset.UtcNow;

            dbContext.Add(data);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return data;
        }

        public async Task<FlightData> PatchAsync(string id, FlightData data)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var flight = await dbContext.Flights.FindAsync(id).ConfigureAwait(false);
            if (data.Title != null) flight.Title = data.Title;
            if (data.Description != null) flight.Description = data.Description;
            if (data.Aircraft != null) flight.Aircraft = data.Aircraft;
            if (data.Airline != null) flight.Airline = data.Airline;
            if (data.FlightNumber != null) flight.FlightNumber = data.FlightNumber;
            if (data.VideoUrl != null) flight.VideoUrl = data.VideoUrl;

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return flight;
        }

        public async Task<FlightData> InsertOrUpdateFlightAsync(string id, FlightData data)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var flight = await dbContext.Flights.FindAsync(id).ConfigureAwait(false);

            if (flight == null)
            {
                data.AddedDateTime = DateTimeOffset.UtcNow;
                dbContext.Add(data);

                flight = data;

                logger.LogDebug("Flight is inserted.");
            }
            else
            {
                flight.CopyFrom(data);
                // NOTE: work around for adding Owned entity
                dbContext.Entry(flight).State = EntityState.Modified;
                if (flight.StatusTakeOff != null && dbContext.Entry(flight.StatusTakeOff).State == EntityState.Added)
                    dbContext.Entry(flight.StatusTakeOff).State = EntityState.Modified;
                if (flight.StatusLanding != null && dbContext.Entry(flight.StatusLanding).State == EntityState.Added)
                    dbContext.Entry(flight.StatusLanding).State = EntityState.Modified;

                logger.LogDebug("Flight is updated.");
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return flight;
        }

        public async Task<bool> DeleteFlightAsync(string id)
        {
            var flight = await dbContext.Flights.FindAsync(id).ConfigureAwait(false);
            if (flight == null) return false;

            dbContext.Flights.Remove(flight);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            return true;
        }

        public async Task<IEnumerable<FlightStatus>> GetRouteAsync(string id)
        {
            var route = await dbContext.FlightStatuses.Where(o => o.FlightId == id).ToListAsync().ConfigureAwait(false);
            return route.Select(o => o.ToDTO());
        }

        private static SemaphoreSlim sm = new SemaphoreSlim(1);

        public async Task UpdateRouteAsync(string id, List<FlightStatus> route)
        {
            try
            {
                await sm.WaitAsync().ConfigureAwait(false);
                logger.LogDebug("Start updating route");

                var last = await dbContext.FlightStatuses
                    .Where(o => o.FlightId == id)
                    .Select(o => o.SimTime)
                    .Cast<double?>()
                    .MaxAsync().ConfigureAwait(false) ?? 0;

                foreach (var status in route.OrderBy(o => o.SimTime).Where(o => o.SimTime > last))
                {
                    if (status.SimTime > last)
                    {
                        last = status.SimTime;
                        dbContext.FlightStatuses.Add(new FlightStatusWrapper(id, status));
                    }
                }
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                logger.LogDebug("Finish updating route");
            }
            finally
            {
                sm.Release();
            }
        }

        public async IAsyncEnumerable<AircraftData> GetAircraftsAsync()
        {
            var groups = dbContext.Flights
                .Where(o => o.Aircraft != null)
                .Select(o => o.Aircraft)
                .AsNoTracking()
                .AsEnumerable()
                .GroupBy(o => o.TailNumber);

            foreach (var group in groups)
            {
                var aircraft = group.First();
                yield return aircraft;
            }
        }

        public async Task<AircraftData> GetAircraftAsync(string tailNumber)
        {
            var flight = await dbContext.Flights.FirstOrDefaultAsync(o => o.Aircraft.TailNumber == tailNumber).ConfigureAwait(false);

            return flight?.Aircraft;
        }

        public async Task<List<string>> GetAircraftPictureUrlsAsync(string tailNumber)
        {
            var flights = await dbContext.Flights.Where(o => o.Aircraft.TailNumber == tailNumber).ToListAsync().ConfigureAwait(false);
            var flightIds = flights.Select(o => o.Id);

            return await dbContext.FlightStatuses
                .Where(o => flightIds.Contains(o.FlightId))
                .Where(o => !string.IsNullOrEmpty(o.ScreenshotUrl))
                .Select(o => o.ScreenshotUrl).ToListAsync().ConfigureAwait(false);
        }
    }
}
