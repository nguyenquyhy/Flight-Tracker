using FlightTracker.Clients.Logics;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlightTracker.Clients.WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly SignalRLogic signalR;
        private readonly FlightLogic flightLogic;
        private readonly TestLogic testLogic;
        private readonly IFlightStatusUpdater flightStatusUpdater;
        private readonly FlightInfoViewModel viewModel;
        private FileSystemWatcher watcher;

        public MainWindow(
            FlightInfoViewModel viewModel,
            SignalRLogic signalR,
            FlightLogic flightLogic,
            TestLogic testLogic,
            IAircraftDataUpdater aircraftDataUpdater,
            IFlightPlanUpdater flightPlanUpdater,
            IFlightStatusUpdater flightStatusUpdater)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.signalR = signalR;
            this.flightLogic = flightLogic;
            this.testLogic = testLogic;
            this.flightStatusUpdater = flightStatusUpdater;
            DataContext = viewModel;

            aircraftDataUpdater.AircraftDataUpdated += AircraftDataUpdater_AircraftDataUpdated;
            flightPlanUpdater.FlightPlanUpdated += FlightPlanUpdater_FlightPlanUpdated;
            flightStatusUpdater.FlightStatusUpdated += FlightStatusUpdater_FlightStatusUpdated;

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Prepar3D v4 Files");

            watcher = new FileSystemWatcher(folder);
            watcher.Created += Watcher_Created;
            watcher.EnableRaisingEvents = true;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                try
                {
                    viewModel.WebConnectionState = ConnectionState.Connecting;
                    await signalR.StartAsync();
                    viewModel.WebConnectionState = ConnectionState.Connected;
                    break;
                }
                catch
                {
                    viewModel.WebConnectionState = ConnectionState.Failed;
                    await Task.Delay(3000);
                }
            }
        }

        private void AircraftDataUpdater_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            viewModel.Update(e.Data);
        }

        private void FlightPlanUpdater_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            viewModel.Update(e.FlightPlan);
        }

        private void FlightStatusUpdater_FlightStatusUpdated(object sender, FlightStatusUpdatedEventArgs e)
        {
            viewModel.Update(e.FlightStatus);
        }

        private void ButtonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            flightStatusUpdater.Screenshot();
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            await flightLogic.ScreenshotAsync(Path.GetFileName(e.FullPath), File.ReadAllBytes(e.FullPath)).ConfigureAwait(false);
        }

        private async void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            await flightLogic.SaveAsync();
        }

        private async void ButtonSaveAndNew_Click(object sender, RoutedEventArgs e)
        {
            await flightLogic.NewFlightAsync(false, viewModel.Title);
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            flightLogic.UpdateTitle(viewModel.Title);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
        }

        private async void ButtonTestStorage_Click(object sender, RoutedEventArgs e)
        {
            await testLogic.TestImageUploader();
        }
    }
}
