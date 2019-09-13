using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public interface IImageUploader
    {
        Task<List<string>> ListAsync();
        Task<string> UploadAsync(string name, byte[] image);
        Task DeleteAsync(string name);
    }
}
