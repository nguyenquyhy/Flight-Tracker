using System.Threading.Tasks;

namespace FlightTracker.Clients.Logics
{
    public interface IImageUploader
    {
        Task<string> UploadAsync(string name, byte[] image);
    }
}
