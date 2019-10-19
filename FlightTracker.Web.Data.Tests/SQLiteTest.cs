using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data.Tests
{
    [TestClass]
    public class SQLiteTest
    {
        private readonly IIdProvider idProvider = new GuidIdProvider();

        [TestMethod]
        public async Task SetStatusTakeOffOfUnmodifiedFlight()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseInMemoryDatabase("FlightTracker").Options;
            var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite(connection).Options;
            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlServer("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=FlightTracker;Integrated Security=SSPI;").Options;

            using (var dbContext = new SqliteDbContext(options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            var flight = new DTOs.FlightData
            {
                Title = "Test 1"
            };

            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<SqliteFlightStorage>();

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                flight = await storage.AddFlightAsync(flight);

                flight.StatusTakeOff = new DTOs.FlightStatus
                {
                    SimTime = 1000,
                    Altitude = 0.5
                };

                await storage.InsertOrUpdateFlightAsync(flight.Id, flight);

                // Set this to make sure that the object inmemory is changed while the object in database is not
                flight.Title = "1";
                flight.StatusTakeOff.SimTime = 2000;
                flight.StatusTakeOff.SimTime = 1;
            }

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                var flight2 = await storage.GetFlightAsync(flight.Id);

                Assert.IsNotNull(flight2);
                Assert.IsNotNull(flight2.StatusTakeOff);
                Assert.IsNull(flight2.StatusLanding);
                Assert.AreEqual("Test 1", flight2.Title);
                Assert.AreEqual(1000, flight2.StatusTakeOff.SimTime);
                Assert.AreEqual(0.5, flight2.StatusTakeOff.Altitude);
            }
        }

        [TestMethod]
        public async Task SetStatusTakeOffOfModifiedFlight()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseInMemoryDatabase("FlightTracker").Options;
            var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite(connection).Options;
            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlServer("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=FlightTracker;Integrated Security=SSPI;").Options;

            using (var dbContext = new SqliteDbContext(options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            var flight = new DTOs.FlightData
            {
                Title = "Test 1"
            };

            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<SqliteFlightStorage>();

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                flight = await storage.AddFlightAsync(flight);

                flight.AirportFrom = "VVTS";
                flight.StatusTakeOff = new DTOs.FlightStatus
                {
                    SimTime = 1000,
                    Altitude = 0.5
                };

                await storage.InsertOrUpdateFlightAsync(flight.Id, flight);

                // Set this to make sure that the object inmemory is changed while the object in database is not
                flight.Title = "1";
                flight.StatusTakeOff.SimTime = 2000;
                flight.StatusTakeOff.SimTime = 1;
            }

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                var flight2 = await storage.GetFlightAsync(flight.Id);

                Assert.IsNotNull(flight2);
                Assert.IsNotNull(flight2.StatusTakeOff);
                Assert.IsNull(flight2.StatusLanding);
                Assert.AreEqual("Test 1", flight2.Title);
                Assert.AreEqual("VVTS", flight2.AirportFrom);
                Assert.AreEqual(1000, flight2.StatusTakeOff.SimTime);
                Assert.AreEqual(0.5, flight2.StatusTakeOff.Altitude);
            }
        }

        [TestMethod]
        public async Task SetStatusTakeOffOfAddedFlight()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseInMemoryDatabase("FlightTracker").Options;
            var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlite(connection).Options;
            //var options = new DbContextOptionsBuilder<SqliteDbContext>().UseSqlServer("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=FlightTracker;Integrated Security=SSPI;").Options;

            using (var dbContext = new SqliteDbContext(options))
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
            }

            var flight = new DTOs.FlightData
            {
                Title = "Test 1",
                StatusTakeOff = new DTOs.FlightStatus
                {
                    SimTime = 1000,
                    Altitude = 0.5
                }
            }; 
            
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<SqliteFlightStorage>();

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                flight = await storage.AddFlightAsync(flight);

                // Set this to make sure that the object inmemory is changed while the object in database is not
                flight.Title = "1";
                flight.StatusTakeOff = null;
            }

            using (var dbContext = new SqliteDbContext(options))
            {
                var storage = new SqliteFlightStorage(dbContext, idProvider, logger);

                var flight2 = await storage.GetFlightAsync(flight.Id);

                Assert.IsNotNull(flight2);
                Assert.IsNotNull(flight2.StatusTakeOff);
                Assert.IsNull(flight2.StatusLanding);
                Assert.AreEqual("Test 1", flight2.Title);
                Assert.AreEqual(1000, flight2.StatusTakeOff.SimTime);
                Assert.AreEqual(0.5, flight2.StatusTakeOff.Altitude);
            }
        }
    }
}
