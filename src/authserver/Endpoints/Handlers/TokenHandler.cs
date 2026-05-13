using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TB.Auth.Web.Endpoints.Handlers;

public static class TokenHandler
{
    public static async Task<IResult> HandleAsync(HttpContext context, IOpenIddictScopeManager scopeManager)
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