using FlightTracker.DTOs;
using Newtonsoft.Json;

namespace FlightTracker.Clients.Logics
{
    /// <summary>
    /// This class contains the properties that should not be stored on server and will not be sent to server.
    /// </summary>
    public class ClientFlightStatus : FlightStatus
    {
        [JsonIgnore]
        public int SimRate { get; set; }
    }
}
