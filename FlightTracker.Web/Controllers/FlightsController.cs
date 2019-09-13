using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlightTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private readonly IFlightStorage flightStorage;

        public FlightsController(IFlightStorage flightStorage)
        {
            this.flightStorage = flightStorage;
        }

        [HttpGet]
        public async Task<IEnumerable<FlightData>> Get(int? limit = null)
        {
            var data = await flightStorage.GetAllAsync().ConfigureAwait(true);
            data = JsonConvert.DeserializeObject<List<FlightData>>(JsonConvert.SerializeObject(data));
            foreach (var flight in data)
            {
                flight.Statuses = null;
            }

            if (limit != null && data.Count() > limit.Value)
            {
                data = data.OrderByDescending(a => a.AddedDateTime).Take(limit.Value);
            }
            return data;
        }

        [HttpGet("{id}", Name = "Get")]
        public Task<FlightData> Get(string id)
        {
            return flightStorage.GetAsync(id);
        }

        [HttpPost]
        public Task<FlightData> Post([FromBody] FlightData flightData)
        {
            return flightStorage.AddAsync(flightData);
        }

        [HttpPut("{id}")]
        public Task<FlightData> Put(string id, [FromBody] FlightData flightData)
        {
            return flightStorage.UpdateAsync(id, flightData);
        }

        [HttpPatch("{id}")]
        public Task<FlightData> Patch(string id, [FromBody] FlightData flightData)
        {
            return flightStorage.PatchAsync(id, flightData);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public Task Delete(string id)
        {
            return flightStorage.DeleteAsync(id);
        }
    }
}
