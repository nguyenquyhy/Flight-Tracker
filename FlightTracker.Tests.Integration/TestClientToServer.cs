using FlightTracker.Clients.Logics;
using FlightTracker.Web.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;

namespace FlightTracker.Tests.Integration
{
    [TestClass]
    public class TestClientToServer
    {
        [TestMethod]
        public async Task TestSaveFromClient()
        {
            var (webApplication, connection) = SetupServer();
            using (connection)
            {
                var httpClient = webApplication.CreateClient();

                var flightAPI = new FlightsAPIClient(Options.Create(new AppSettings
                {
                    BaseUrl = ""
                }), httpClient);
                var flightSimInterface = Mock.Of<IFlightSimInterface>();
                var imageUploader = Mock.Of<IImageUploader>();
                var flightLogic = new FlightLogic(new LoggerFactory().CreateLogger<FlightLogic>(), flightAPI, flightSimInterface, imageUploader);

                flightLogic.FlightData.Aircraft = new DTOs.AircraftData
                {
                    Title = "Test aircraft",
                    Type = "Test aircraft type",
                    Model = "Test aircraft model",
                    TailNumber = "123",
                    Airline = "",
                    FlightNumber = ""
                };

                flightLogic.FlightRoute.Add(new ClientFlightStatus
                {
                    SimTime = 0.1,
                    SimRate = 1,
                });
                var result = await flightLogic.SaveAsync();

                result.Should().BeTrue();
            }
        }

        private static (WebApplicationFactory<Web.Startup>, SqliteConnection) SetupServer()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var webApplication = new WebApplicationFactory<Web.Startup>()
                .WithWebHostBuilder(config =>
                {
                    config.ConfigureServices(services =>
                    {
                        services.AddDbContext<SqliteDbContext>(options => options.UseSqlite(connection));
                    });
                });
            webApplication.Server.AllowSynchronousIO = true;
            using (var serviceScope = webApplication.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDbContext>();
                context.Database.EnsureCreated();
            }

            return (webApplication, connection);
        }
    }
}
