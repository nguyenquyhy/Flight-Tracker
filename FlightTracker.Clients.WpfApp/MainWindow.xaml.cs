using FlightTracker.Clients.Logics;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace FlightTracker.Clients.WpfApp
{
    public partial class MainWindow : Window
    {
        private readonly FlightInfoViewModel viewModel;
        private readonly SignalRLogic signalR;
        private readonly FlightLogic flightLogic;
        private readonly TestLogic testLogic;
        private readonly IFlightSimInterface flightSimInterface;
        private readonly FileWatcherLogic watcher;
        private readonly IStorageLogic storageLogic;
        private readonly ILogger<MainWindow> logger;

        public MainWindow(
            FlightInfoViewModel viewModel,
            SignalRLogic signalR,
            FlightLogic flightLogic,
            TestLogic testLogic,
            IFlightSimInterface flightSimInterface,
            FileWatcherLogic watcher,
            IStorageLogic storageLogic,
            ILogger<MainWindow> logger)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.signalR = signalR;
            this.flightLogic = flightLogic;
            this.testLogic = testLogic;
            this.flightSimInterface = flightSimInterface ?? throw new ArgumentNullException(nameof(flightSimInterface));
            this.watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
            this.storageLogic = storageLogic ?? throw new ArgumentNullException(nameof(storageLogic));
            this.logger = logger;
            DataContext = viewModel;

            flightSimInterface.AircraftDataUpdated += FlightSimInterface_AircraftDataUpdated;
            flightSimInterface.FlightPlanUpdated += FlightSimInterface_FlightPlanUpdated;
            flightSimInterface.FlightStatusUpdated += FlightStatusUpdater_FlightStatusUpdated;

            watcher.FileCreated += Watcher_FileCreated;

            TextArchiveFolder.Text = storageLogic.ArchiveFolder;
        }

        public void DisplayLog(LogWrapper log)
        {
            if (log == null) return;
            try
            {
                var content = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] [{log.LogLevel.ToString()}] {log.FormattedString}";
                var ex = log.Exception;
                while (ex != null)
                {
                    content += $"\n Error: [{ex.GetType().FullName}] {ex.Message}\n Stack Trace: {ex.StackTrace}";
                    if (ex.InnerException != null)
                    {
                        content += "\n ==== INNER EXCEPTION ====";
                    }
                    ex = ex.InnerException;
                }
                TextLog.AppendText(content + "\n");
            }
            catch
            {

            }
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

        private void FlightSimInterface_AircraftDataUpdated(object sender, AircraftDataUpdatedEventArgs e)
        {
            viewModel.Update(e.Data);
        }

        private void FlightSimInterface_FlightPlanUpdated(object sender, FlightPlanUpdatedEventArgs e)
        {
            viewModel.Update(e.FlightPlan);
        }

        private void FlightStatusUpdater_FlightStatusUpdated(object sender, FlightStatusUpdatedEventArgs e)
        {
            viewModel.Update(e.FlightStatus);

            ButtonAddStatus.Visibility = flightLogic.FlightRoute.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ButtonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            flightSimInterface.Screenshot();
        }

        private void ButtonDecreaseSimRate_Click(object sender, RoutedEventArgs e)
        {
            flightSimInterface.DecreaseSimRate();
        }

        private void ButtonIncreaseSimRate_Click(object sender, RoutedEventArgs e)
        {
            flightSimInterface.IncreaseSimRate();
        }

        private async void Watcher_FileCreated(object sender, FileCreatedEventArgs e)
        {
            var uploaded = await flightLogic.UploadScreenshotAsync(Path.GetFileName(e.FilePath), File.ReadAllBytes(e.FilePath)).ConfigureAwait(true);
            if (uploaded && !string.IsNullOrWhiteSpace(storageLogic.ArchiveFolder) && Directory.Exists(storageLogic.ArchiveFolder))
            {
                File.Move(e.FilePath, Path.Combine(storageLogic.ArchiveFolder, Path.GetFileName(e.FilePath)));
            }
        }

        private async void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            await flightLogic.SaveAsync().ConfigureAwait(true);
        }

        private async void ButtonSaveAndNew_Click(object sender, RoutedEventArgs e)
        {
            await flightLogic.NewFlightAsync(FlightLogic.NewFlightReason.UserRequest, viewModel.Title).ConfigureAwait(true);
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
            await testLogic.TestImageUploader().ConfigureAwait(true);
        }

        private async void ButtonDumpFlight_Click(object sender, RoutedEventArgs e)
        {
            await flightLogic.DumpAsync().ConfigureAwait(true);
        }

        private void ButtonSelectArchive_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.InitialDirectory = watcher.ScreenshotFolderPath;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    if (dialog.FileName == watcher.ScreenshotFolderPath)
                    {
                        MessageBox.Show("Please choose a folder different from the default screenshot folder!");
                    }
                    else
                    {
                        TextArchiveFolder.Text = dialog.FileName;
                        storageLogic.ArchiveFolder = dialog.FileName;
                    }
                }
            }
        }

        private void ButtonAddStatus_Click(object sender, RoutedEventArgs e)
        {
            flightLogic.AddNextStatus();
        }
    }
}
