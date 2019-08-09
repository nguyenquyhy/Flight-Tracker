using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlightTracker.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigsController : ControllerBase
    {
        private readonly AppSettings appSettings;

        public ConfigsController(IOptions<AppSettings> appSettings)
        {
            this.appSettings = appSettings.Value;
        }

        [HttpGet]
        public AppSettings Get()
        {
            return appSettings;
        }
    }
}
