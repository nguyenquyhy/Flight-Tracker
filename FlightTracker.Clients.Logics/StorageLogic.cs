using Newtonsoft.Json;
using System;
using System.IO;

namespace FlightTracker.Clients.Logics
{
    public class StorageLogic : IStorageLogic
    {
        private const string SettingsFileName = "settings.json";

        private readonly string storageFolder;

        public StorageLogic()
        {
            storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".flighttracker");
            if (!Directory.Exists(storageFolder)) Directory.CreateDirectory(storageFolder);
        }


        public string ArchiveFolder
        {
            get => LoadSettings().ArchiveFolder;
            set
            {
                var settings = LoadSettings();
                settings.ArchiveFolder = value;
                SaveSettings(settings);
            }
        }

        private Settings LoadSettings()
        {
            var settingsFilePath = Path.Combine(storageFolder, SettingsFileName);

            if (File.Exists(settingsFilePath))
            {
                var dataString = File.ReadAllText(settingsFilePath);
                return JsonConvert.DeserializeObject<Settings>(dataString);
            }
            else
            {
                return new Settings();
            }
        }

        private void SaveSettings(Settings settings)
        {
            var settingsFilePath = Path.Combine(storageFolder, SettingsFileName);
            File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(settings));
        }
    }

    public class Settings
    {
        public string ArchiveFolder { get; set; }
    }
}