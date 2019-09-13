using System.Collections.Generic;

namespace FlightTracker.Web.Models
{
    public class ConfigViewModel
    {
        public string GoogleMapsKey { get; set; }

        public Dictionary<string, PermissionViewModel> Permissions { get; } = new Dictionary<string, PermissionViewModel>();
    }

    public class PermissionViewModel
    {
        public bool Edit { get; set; }
        public bool Delete { get; set; }
    }
}
