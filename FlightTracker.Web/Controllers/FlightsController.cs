using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("{id}/Route", Name = "Post Route")]
        public Task PostRoute(string id, [FromBody] List<FlightStatus> route)
        {
            return flightStorage.UpdateRouteAsync(id, route);
        }
    }
}
