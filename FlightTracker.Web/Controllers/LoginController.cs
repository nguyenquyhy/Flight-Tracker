using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FlightTracker.Web.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        public IActionResult Index([FromQuery]string ReturnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("~/");

            ViewBag.ReturnUrl = ReturnUrl;
            return View();
        }

        [HttpGet("Login/Google")]
        public IActionResult LoginGoogle([FromQuery]string ReturnUrl)
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("~/");

            return Challenge(new AuthenticationProperties
            {
                RedirectUri = ReturnUrl
            }, "Google");
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("~/");
        }
    }
}