using FlightTracker.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

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
        public ConfigViewModel Get()
        {
            return new ConfigViewModel
            {
                GoogleMapsKey = appSettings.GoogleMapsKey,
                Permissions =
                {
                    ["Flight"] = new PermissionViewModel
                    {
                        Delete = User.Identity.IsAuthenticated
                    }
                }
            };
        }
    }
}
