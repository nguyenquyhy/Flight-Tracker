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
        Task<bool> DeleteAsync(string id);
    }
}
