using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data.Migrators
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug))
                .AddSingleton<IIdProvider, GuidIdProvider>()
                .AddSingleton(provider =>
                {
                    var sqliteDbContext = new SqliteDbContext(new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite("Data Source=flights.db").Options);
                    sqliteDbContext.Database.EnsureCreated();
                    return sqliteDbContext;
                })
                .AddSingleton<SqliteFlightStorage>()
                .AddSingleton(provider => new JsonFileFlightStorage(provider.GetService<IIdProvider>(), "flights.json"))
                .BuildServiceProvider();

            var jsonStorage = serviceProvider.GetService<JsonFileFlightStorage>();
            var sqliteStorage = serviceProvider.GetService<SqliteFlightStorage>();

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
