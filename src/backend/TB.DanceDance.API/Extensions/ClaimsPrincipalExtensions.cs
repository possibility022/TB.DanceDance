using System.Security.Claims;

namespace TB.DanceDance.API.Extensions;

internal static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public string GetSubject()
        {
            string? user = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (user == null)
                throw new Exception("Subject claim not found.");
            return user;
        }

        public string? TryGetSubject()
        {
            return claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
        }
    }
}
