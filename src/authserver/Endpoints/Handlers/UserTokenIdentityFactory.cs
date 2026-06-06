using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using TB.Auth.Web.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TB.Auth.Web.Endpoints.Handlers;

/// <summary>
/// Builds the OpenIddict <see cref="ClaimsPrincipal"/> issued for a user, with claims,
/// destinations, scopes and resources. Shared by the authorization-code flow
/// (<see cref="ConnectAuthorizeHandler"/>) and the dev-only password grant
/// (<see cref="TokenHandler"/>) so the token shape stays identical across flows.
/// </summary>
public static class UserTokenIdentityFactory
{
    public static async Task<ClaimsPrincipal> BuildAsync(
        User user,
        UserManager<User> userManager,
        IEnumerable<string> scopes,
        IOpenIddictScopeManager scopeManager)
    {
        var userClaims = await userManager.GetClaimsAsync(user);

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(
            new Claim(Claims.Subject, user.Id).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var username = user.UserName ?? user.Email ?? user.Id;
        identity.AddClaim(
            new Claim(Claims.PreferredUsername, username).SetDestinations(Destinations.AccessToken,
                Destinations.IdentityToken));

        var name = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value ?? username;
        identity.AddClaim(
            new Claim(Claims.Name, name).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));

        var email = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value ?? user.Email;
        if (!string.IsNullOrWhiteSpace(email))
        {
            identity.AddClaim(
                new Claim(Claims.Email, email).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
        }

        var givenName = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value;
        if (!string.IsNullOrWhiteSpace(givenName))
        {
            identity.AddClaim(new Claim(Claims.GivenName, givenName).SetDestinations(Destinations.AccessToken,
                Destinations.IdentityToken));
        }

        var familyName = userClaims.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)?.Value;
        if (!string.IsNullOrWhiteSpace(familyName))
        {
            identity.AddClaim(new Claim(Claims.FamilyName, familyName).SetDestinations(Destinations.AccessToken,
                Destinations.IdentityToken));
        }

        identity.SetScopes(scopes);
        identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        return new ClaimsPrincipal(identity);
    }
}
