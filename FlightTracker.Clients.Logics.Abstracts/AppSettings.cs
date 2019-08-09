namespace FlightTracker.Clients.Logics
{
    public class AppSettings
    {
        public string BaseUrl { get; set; }
        public AppSettingsAzureStorage AzureStorage { get; set; }
    }

    public class AppSettingsAzureStorage
    {
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
    }
}