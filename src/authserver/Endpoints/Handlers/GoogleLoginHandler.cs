using System.Security.Claims;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Client.WebIntegration;
using TB.Auth.Web.Identity;

namespace TB.Auth.Web.Endpoints.Handlers;

public static class GoogleLoginHandler
{
    public static async Task<IResult> HandleAsync(HttpContext context, UserManager<User> userManager)
    {
        var result = await context.AuthenticateAsync(OpenIddictClientWebIntegrationConstants.Providers.Google);
        if (result.Succeeded != true || result.Principal is null)
        {
            return Results.BadRequest("External authentication error.");
        }

        var providerUserId = result.Principal.FindFirst("sub")?.Value ??
                             result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(providerUserId))
        {
            return Results.BadRequest("Google subject claim is missing.");
        }

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ??
                    result.Principal.FindFirst(OpenIddictConstants.Claims.Email)?.Value;
        var givenName = result.Principal.FindFirst(ClaimTypes.GivenName)?.Value ??
                        result.Principal.FindFirst(OpenIddictConstants.Claims.GivenName)?.Value;
        var surname = result.Principal.FindFirst(ClaimTypes.Surname)?.Value ??
                      result.Principal.FindFirst(OpenIddictConstants.Claims.FamilyName)?.Value;
        var displayName = result.Principal.FindFirst(ClaimTypes.Name)?.Value ??
                          result.Principal.FindFirst(OpenIddictConstants.Claims.Name)?.Value ??
                          email ??
                          providerUserId;

        var user = await userManager.FindByIdAsync(providerUserId);
        if (user is null)
        {
            user = new User { Id = providerUserId, UserName = email ?? providerUserId, Email = email };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Results.BadRequest(string.Join(" | ", createResult.Errors.Select(error => error.Description)));
            }
        }

        var claimsToAdd = new List<Claim>();
        var existingClaims = await userManager.GetClaimsAsync(user);

        AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Email, email);
        AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Name, displayName);
        AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.GivenName, givenName);
        AddClaimIfMissing(existingClaims, claimsToAdd, ClaimTypes.Surname, surname);

        if (claimsToAdd.Count > 0)
        {
            await userManager.AddClaimsAsync(user, claimsToAdd);
        }

        var identity = new ClaimsIdentity(
            authenticationType: CookieAuthenticationDefaults.AuthenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id));
        identity.AddClaim(new Claim(ClaimTypes.Name, displayName));

        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        var redirectUri = result.Properties?.RedirectUri;
        if (string.IsNullOrWhiteSpace(redirectUri) &&
            result.Properties?.Items.TryGetValue("return_url", out var returnUrl) == true)
        {
            redirectUri = returnUrl;
        }

        var properties = new AuthenticationProperties();
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            properties.RedirectUri = redirectUri;
        }

        return Results.SignIn(
            new ClaimsPrincipal(identity),
            properties,
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    static void AddClaimIfMissing(IEnumerable<Claim> existingClaims, IList<Claim> newClaims, string type, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var exists = existingClaims.Any(claim => claim.Type == type && claim.Value == value);
        if (!exists)
        {
            newClaims.Add(new Claim(type, value));
        }
    }
}