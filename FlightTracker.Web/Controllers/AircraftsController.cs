using FlightTracker.DTOs;
using FlightTracker.Web.Data;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<IEnumerable<AircraftData>> Get()
        {
            var flights = await flightStorage.GetAllAsync();
            var aircrafts = flights.Where(o => o.Aircraft != null).Select(o => o.Aircraft).ToList();

            var tailNumbers = aircrafts.Select(o => o.TailNumber).Distinct();

            return tailNumbers.Select(o => aircrafts.First(a => a.TailNumber == o));
        }
    }
}
