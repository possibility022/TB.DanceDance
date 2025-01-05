using System.Security.Claims;

namespace TB.DanceDance.API.Extensions;

internal static class ClaimsPrincipalExtensions
{
    public static string GetSubject(this ClaimsPrincipal claimsPrincipal)
    {
        string? user = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (user == null)
            throw new Exception("Subject claim not found.");
        return user;
    }
}
