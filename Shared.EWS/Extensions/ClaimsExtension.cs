using System.Security.Claims;

namespace Shared.EWS.Extensions
{
    public static class ClaimsExtension
    {
        public static int GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirst("user_id")?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static int GetRoleId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirst("role_id")?.Value;
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static string GetEmail(this ClaimsPrincipal principal)
            => principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}