using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AircraftsController : ControllerBase
    {
        private readonly IFlightStorage flightStorage;

        public AircraftsController(IFlightStorage flightStorage)
        {
            this.flightStorage = flightStorage;
        }

        [HttpGet]
        public async IAsyncEnumerable<AircraftData> Get()
        {
            await foreach (var aircraft in flightStorage.GetAllAircraftsAsync())
            {
                yield return aircraft;
            }
        }

        [HttpGet]
        [Route("{tailNumber}")]
        public async Task<AircraftData> Get(string tailNumber)
        {
            return await flightStorage.GetAircraftAsync(tailNumber).ConfigureAwait(true);
        }
    }
}
