using TB.Auth.Web.Identity;

namespace TB.Auth.Web.Endpoints.Handlers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Client.WebIntegration.OpenIddictClientWebIntegrationConstants;
using TB.Auth.Web;

public static class ConnectAuthorizeHandler
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        UserManager<User> userManager,
        IOpenIddictScopeManager scopeManager,
        AuthServerOptions authOptions)
    {
        var principal = (await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme))?.Principal;
        if (principal is not { Identity.IsAuthenticated: true })
        {
            if (authOptions.AllowWeakPasswords)
            {
                var devReturnUrl = context.Request.GetEncodedUrl();
                return Results.Redirect($"/dev/login?returnUrl={Uri.EscapeDataString(devReturnUrl)}");
            }

            var googleReturnUrl = context.Request.GetEncodedUrl();
            var properties = new AuthenticationProperties { RedirectUri = googleReturnUrl };
            properties.Items["return_url"] = googleReturnUrl;

            return Results.Challenge(properties, [Providers.Google]);
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                     principal.FindFirst(Claims.Subject)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest("Authenticated user does not have a subject identifier.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Results.BadRequest("Authenticated user was not found in identity store.");
        }

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

        identity.SetScopes(await ResolveScopesAsync(context));
        identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    static async Task<IEnumerable<string>> ResolveScopesAsync(HttpContext context)
    {
        var scope = context.Request.Query["scope"].ToString();
        if (!string.IsNullOrWhiteSpace(scope))
        {
            return ScopeParser.ParseScopes(scope);
        }

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            return ScopeParser.ParseScopes(form["scope"].ToString());
        }

        return [];
    }
}