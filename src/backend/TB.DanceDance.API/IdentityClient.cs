using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TB.DanceDance.API;

public class IdentityClient : IIdentityClient
{
    public Task<IdentityUserInfo> GetNameAsync(string accessToken, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new AppException("Access token is empty.");
        }

        JwtSecurityToken jwtToken;
        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        }
        catch
        {
            throw new AppException("Access token is not a valid JWT.");
        }

        var subject = GetClaimValue(jwtToken, "sub", ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new AppException("sub claim not found in a token");
        }

        var preferredUsername = GetClaimValue(jwtToken, "preferred_username", ClaimTypes.Name);
        var fullName = GetClaimValue(jwtToken, "name", ClaimTypes.Name);
        var firstName = GetClaimValue(jwtToken, "given_name", ClaimTypes.GivenName);
        var lastName = GetClaimValue(jwtToken, "family_name", ClaimTypes.Surname);

        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(fullName))
        {
            firstName = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(fullName))
        {
            var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length > 1)
            {
                lastName = string.Join(' ', nameParts.Skip(1));
            }
        }

        var email = GetClaimValue(jwtToken, "email", ClaimTypes.Email);

        var user = new IdentityUserInfo
        {
            Id = subject,
            FirstName = firstName ?? preferredUsername ?? "User",
            LastName = lastName ?? string.Empty,
            Email = email ?? preferredUsername ?? $"{subject}@local"
        };

        return Task.FromResult(user);
    }

    private static string? GetClaimValue(JwtSecurityToken jwtToken, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = jwtToken.Claims
                .FirstOrDefault(claim => string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

public interface IIdentityClient
{
    // Returns user data resolved from access token claims.
    Task<IdentityUserInfo> GetNameAsync(string accessToken, CancellationToken token);
}

/// <summary>User identity details resolved from the bearer token claims.</summary>
public record IdentityUserInfo
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
}
