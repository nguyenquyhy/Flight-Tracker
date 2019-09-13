using FlightTracker.Clients.Logics;
using FlightTracker.Clients.Logics.AzureStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FlightTracker.Clients.WpfApp
{
    public partial class App : Application
    {
        private Logger logger;

        public ServiceProvider ServiceProvider { get; private set; }
        public IConfigurationRoot Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            logger = new LoggerConfiguration().WriteTo.File("flighttracker.log").CreateLogger();

            services.AddOptions();
            services.Configure<AppSettings>((appSettings) =>
            {
                Configuration.GetSection("AppSettings").Bind(appSettings);
            });
            services.AddLogging(configure =>
            {
                configure
                    .AddDebug()
                    .AddSerilog();
            });
            services.AddSingleton<FlightInfoViewModel>();
            services.AddSingleton<SignalRLogic>();
            services.AddSingleton<SimConnectLogic>();
            services.AddSingleton<IEnvironmentDataUpdater>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton<IAirportUpdater>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton<IAircraftDataUpdater>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton<IFlightPlanUpdater>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton<IFlightStatusUpdater>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton<FlightsAPIClient>();
            services.AddSingleton<FlightLogic>();
            services.AddSingleton<IImageUploader, AzureImageUploader>();
            services.AddSingleton<TestLogic>();

            services.AddTransient(typeof(MainWindow));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize SimConnect
            var simConnect = ServiceProvider.GetService<SimConnectLogic>();
            if (simConnect != null)
            {
                // Create an event handle for the WPF window to listen for SimConnect events
                var Handle = new WindowInteropHelper(sender as Window).Handle; // Get handle of main WPF Window
                var HandleSource = HwndSource.FromHwnd(Handle); // Get source of handle in order to add event handlers to it
                HandleSource.AddHook(simConnect.HandleSimConnectEvents);
                var viewModel = ServiceProvider.GetService<FlightInfoViewModel>();

                while (true)
                {
                    try
                    {
                        viewModel.SimConnectionState = ConnectionState.Connecting;
                        simConnect.Initialize(Handle);
                        viewModel.SimConnectionState = ConnectionState.Connected;
                        break;
                    }
                    catch (COMException)
                    {
                        viewModel.SimConnectionState = ConnectionState.Failed;
                        await Task.Delay(5000);
                    }
                }
            }
        }
    }
}
