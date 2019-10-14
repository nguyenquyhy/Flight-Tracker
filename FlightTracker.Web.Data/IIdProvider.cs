using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public interface IIdProvider
    {
        Task<string> GenerateAsync();
    }
}
