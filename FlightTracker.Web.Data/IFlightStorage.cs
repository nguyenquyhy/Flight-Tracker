using FlightTracker.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public interface IFlightStorage
    {
        Task<IEnumerable<FlightData>> GetAllAsync();
        Task<FlightData> GetAsync(string id);
        Task<FlightData> AddAsync(FlightData data);
        Task<FlightData> UpdateAsync(string id, FlightData data);
        Task<FlightData> PatchAsync(string id, FlightData data);
        Task<bool> DeleteAsync(string id);

        IAsyncEnumerable<AircraftData> GetAllAircraftsAsync();
        Task<AircraftData> GetAircraftAsync(string tailNumber);

        Task<IEnumerable<FlightStatus>> GetRouteAsync(string id);
        Task<IEnumerable<FlightStatus>> UpdateRouteAsync(string id, List<FlightStatus> route);
    }
}
