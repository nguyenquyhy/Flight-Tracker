using System.Security.Claims;

namespace FlightTracker.Web
{
    public static class ClaimPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier).Value;
        public static string GetUserName(this ClaimsPrincipal user) => user.FindFirst("name").Value;
        public static string GetUserEmail(this ClaimsPrincipal user) => user.FindFirst(ClaimTypes.Email).Value;
    }
}
