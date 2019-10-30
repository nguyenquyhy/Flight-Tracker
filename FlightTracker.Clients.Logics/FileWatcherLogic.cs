using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public class FileCreatedEventArgs : EventArgs
    {
        public FileCreatedEventArgs(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
    }

    public class FileWatcherLogic : IDisposable
    {
        private readonly FileSystemWatcher watcher;
        private readonly ILogger<FileWatcherLogic> logger;
        private readonly IStorageLogic storageLogic;

        public string ScreenshotFolderPath { get; }

        public event EventHandler<FileCreatedEventArgs> FileCreated;

        public FileWatcherLogic(ILogger<FileWatcherLogic> logger, IStorageLogic storageLogic, string screenshotFolderPath)
        {
            this.logger = logger;
            this.storageLogic = storageLogic;
            ScreenshotFolderPath = screenshotFolderPath;

            watcher = new FileSystemWatcher(screenshotFolderPath);
            watcher.Created += Watcher_Created;
            watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            watcher?.Dispose();
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                logger.LogInformation($"Received created event of '{e.Name}'.");

                if (Directory.Exists(e.FullPath))
                {
                    logger.LogInformation($"Ignore folder '{e.Name}' creation.");
                }
                else
                {
                    if (!File.Exists(e.FullPath))
                    {
                        logger.LogWarning($"Cannot see file '{e.Name}'. Wait for 1s...");
                        await Task.Delay(1000).ConfigureAwait(false);
                    }

                    if (File.Exists(e.FullPath))
                    {
                        while (!IsAvailableForReading(e.FullPath))
                        {
                            logger.LogInformation("File is not available for reading. Wait for 1s...");
                            await Task.Delay(1000);
                        }

                        FileCreated?.Invoke(this, new FileCreatedEventArgs(e.FullPath));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Cannot upload file {e.Name}!");
            }
        }

        private bool IsAvailableForReading(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open,FileAccess.Read))
                {
                    // do something here
                }
                return true;
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, $"File {filePath} cannot be read!");
                return false;
            }
        }
    }
}
