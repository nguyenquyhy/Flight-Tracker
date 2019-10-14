using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data.Migrators
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var idProvider = new GuidIdProvider();
            var jsonStorage = new JsonFileFlightStorage(idProvider, "flights.json");

            var sqliteDbContext = new SqliteDbContext(new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite("Data Source=flights.db").Options);
            sqliteDbContext.Database.EnsureCreated();

            var sqliteStorage = new SqliteFlightStorage(sqliteDbContext, idProvider);

            var flights = await jsonStorage.GetFlightsAsync();
            foreach (var flight in flights)
            {
                await sqliteStorage.InsertOrUpdateFlightAsync(flight.Id, flight);
                var route = await jsonStorage.GetRouteAsync(flight.Id);
                await sqliteStorage.UpdateRouteAsync(flight.Id, route.ToList());
            }
        }
    }
}
