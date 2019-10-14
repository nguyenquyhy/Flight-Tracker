using FlightTracker.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public interface IFlightStorage
    {
        Task<IEnumerable<FlightData>> GetFlightsAsync();
        Task<FlightData> GetFlightAsync(string id);
        Task<FlightData> AddFlightAsync(FlightData data);
        Task<FlightData> InsertOrUpdateFlightAsync(string id, FlightData data);
        Task<FlightData> PatchAsync(string id, FlightData data);
        Task<bool> DeleteFlightAsync(string id);

        IAsyncEnumerable<AircraftData> GetAircraftsAsync();
        Task<AircraftData> GetAircraftAsync(string tailNumber);

        Task<List<string>> GetAircraftPictureUrlsAsync(string tailNumber);

        Task<IEnumerable<FlightStatus>> GetRouteAsync(string id);
        Task UpdateRouteAsync(string id, List<FlightStatus> route);
    }
}
