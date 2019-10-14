using System;
using System.Globalization;
using System.Threading.Tasks;

namespace FlightTracker.Web.Data
{
    public class GuidIdProvider : IIdProvider
    {
        public Task<string> GenerateAsync()
        {
            return Task.FromResult(Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        }
    }
}
