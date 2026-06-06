using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TB.Auth.Web.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TB.Auth.Web.Endpoints.Handlers;

public static class TokenHandler
{
    public static async Task<IResult> HandleAsync(
        HttpContext context,
        IOpenIddictScopeManager scopeManager,
        UserManager<User> userManager,
        AuthServerOptions authOptions)
    {
        var form = context.Request.HasFormContentType
            ? await context.Request.ReadFormAsync()
            : null;

        var grantType = form?["grant_type"].ToString();

        if (string.Equals(grantType, GrantTypes.AuthorizationCode, StringComparison.Ordinal) ||
            string.Equals(grantType, GrantTypes.RefreshToken, StringComparison.Ordinal))
        {
            var authenticationResult =
                await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (authenticationResult.Succeeded != true || authenticationResult.Principal is null)
            {
                return Results.Forbid(authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            return Results.SignIn(authenticationResult.Principal, properties: null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (string.Equals(grantType, GrantTypes.ClientCredentials, StringComparison.Ordinal))
        {
            var clientId = form?["client_id"].ToString();
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = TryReadClientIdFromBasicAuthorizationHeader(context);
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Results.BadRequest("Client identifier cannot be resolved.");
            }

            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.AddClaim(
                new Claim(Claims.Subject, clientId).SetDestinations(Destinations.AccessToken));
            identity.AddClaim(
                new Claim(Claims.Name, clientId).SetDestinations(Destinations.AccessToken));

            identity.SetScopes(ScopeParser.ParseScopes(form?["scope"].ToString()));
            identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Dev-only: Resource Owner Password Credentials grant. Lets any seeded user obtain a
        // token via a single REST call. The flow itself is only registered when
        // AllowWeakPasswords is true (see Configuration.AddServerWithConfiguration); the guard
        // below is defense in depth.
        if (string.Equals(grantType, GrantTypes.Password, StringComparison.Ordinal) &&
            authOptions.AllowWeakPasswords)
        {
            var login = form?["username"].ToString().Trim();
            var password = form?["password"].ToString();

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                return Results.BadRequest("Username and password are required.");
            }

            var user = await userManager.FindByNameAsync(login);
            if (user is null && login.Contains('@'))
            {
                user = await userManager.FindByEmailAsync(login);
            }

            if (user is null || !await userManager.CheckPasswordAsync(user, password))
            {
                return Results.Forbid(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            var principal = await UserTokenIdentityFactory.BuildAsync(
                user, userManager, ScopeParser.ParseScopes(form?["scope"].ToString()), scopeManager);

            return Results.SignIn(principal, properties: null,
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Results.BadRequest("Unsupported grant type.");
    }

    static string? TryReadClientIdFromBasicAuthorizationHeader(HttpContext context)
    {
        var authorizationHeader = context.Request.Headers.Authorization.ToString();
        if (!authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var payload = authorizationHeader["Basic ".Length..].Trim();

        string decoded;
        try
        {
            var bytes = Convert.FromBase64String(payload);
            decoded = System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return null;
        }

        return decoded[..separatorIndex];
    }
}