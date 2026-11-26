using System.Security.Claims;

namespace RevalliDesk.Authorization
{
    /// <summary>Reads the logged-in user's ID and role from the claims in the validated JWT.</summary>
    public static class CurrentUser
    {
        /// <summary>Returns null when nobody is logged in (for example on the login request itself).</summary>
        public static int? GetId(ClaimsPrincipal? user)
        {
            var idText = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idText, out var id) ? id : null;
        }

        /// <summary>The role name from the JWT, for example "SystemAdministrator". Null when nobody is logged in.</summary>
        public static string? GetRole(ClaimsPrincipal? user) => user?.FindFirstValue(ClaimTypes.Role);
    }
}