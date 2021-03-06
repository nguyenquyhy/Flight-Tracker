﻿using FlightTracker.Clients.Logics;
using FlightTracker.Clients.Logics.AzureStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace FlightTracker.Clients.WpfApp
{
    public partial class App : Application
    {
        public ServiceProvider ServiceProvider { get; private set; }

        private MainWindow mainWindow = null;
        private IntPtr Handle;

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

            mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Loaded += MainWindow_Loaded;
            mainWindow.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File("flighttracker.log").CreateLogger();

            services.AddOptions();
            services.Configure<AppSettings>((appSettings) =>
            {
                Configuration.GetSection("AppSettings").Bind(appSettings);
            });
            services.AddLogging(configure =>
            {
                configure
                    .AddDebug()
                    .AddSerilog()
                    .AddProvider(new CustomLoggerProvider(LogLevel.Information, log =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (mainWindow != null && mainWindow.IsLoaded)
                            {
                                mainWindow.DisplayLog(log);
                            }
                        });
                    }));
            });

            services.AddSingleton<FlightInfoViewModel>();
            services.AddSingleton<SignalRLogic>();
            services.AddSingleton<SimConnectLogic>();
            services.AddSingleton<IFlightSimInterface>(provider => provider.GetRequiredService<SimConnectLogic>());
            services.AddSingleton(new HttpClient());
            services.AddSingleton<FlightsAPIClient>();
            services.AddSingleton<FlightLogic>();
            services.AddSingleton<IImageUploader, AzureImageUploader>();
            services.AddSingleton<TestLogic>();
            services.AddSingleton<IStorageLogic, StorageLogic>();
            services.AddSingleton(provider => new FileWatcherLogic(
                provider.GetService<ILogger<FileWatcherLogic>>(), 
                provider.GetService<IStorageLogic>(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Prepar3D v4 Files")));

            services.AddTransient(typeof(MainWindow));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize SimConnect
            var simConnect = ServiceProvider.GetService<SimConnectLogic>();
            if (simConnect != null)
            {
                simConnect.Closed += SimConnect_Closed;

                // Create an event handle for the WPF window to listen for SimConnect events
                Handle = new WindowInteropHelper(sender as Window).Handle; // Get handle of main WPF Window
                var HandleSource = HwndSource.FromHwnd(Handle); // Get source of handle in order to add event handlers to it
                HandleSource.AddHook(simConnect.HandleSimConnectEvents);
                var viewModel = ServiceProvider.GetService<FlightInfoViewModel>();

                await InitializeSimConnectsync(simConnect, viewModel).ConfigureAwait(true);
            }
        }

        private async Task InitializeSimConnectsync(SimConnectLogic simConnect, FlightInfoViewModel viewModel)
        {
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
                    await Task.Delay(5000).ConfigureAwait(true);
                }
            }
        }

        private async void SimConnect_Closed(object sender, EventArgs e)
        {
            var simConnect = ServiceProvider.GetService<SimConnectLogic>();
            var viewModel = ServiceProvider.GetService<FlightInfoViewModel>();
            viewModel.SimConnectionState = ConnectionState.Idle;

            await InitializeSimConnectsync(simConnect, viewModel).ConfigureAwait(true);
        }
    }
}
