using Domain.Exceptions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TB.DanceDance.API;

public class IdentityClient : IIdentityClient
{
    private readonly UserManager<User> userManager;

    public IdentityClient(UserManager<User> userManager)
    {
        this.userManager = userManager;
    }

    public async Task<string?> GetNameAsync(string accessToken, CancellationToken token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(accessToken);
        var tokenS = jsonToken as JwtSecurityToken;
        var sub = tokenS.Claims.FirstOrDefault(r => r.Type == "sub")?.Value;

        if (sub == null)
        {
            throw new AppException("sub claim not found in a token");
        }

        var user = await userManager.FindByIdAsync(sub);

        if (user == null)
        {
            throw new AppException($"user for sub {sub} not found");
        }

        var claims = await userManager.GetClaimsAsync(user);

        var givenNameClaim = claims.FirstOrDefault(r => r.Type == ClaimTypes.Name);

        return givenNameClaim?.Value;
    }
}

public interface IIdentityClient
{
    // Idea of this interface is to provide a service that gives given name based on access token.
    // As token may not contain (and should not) given name claim then access token
    // should be used to request user informations from user info endpoint from oauth.
    // In this application everything is tied together so we have access to UserManager class but interface is design to work without it.


    Task<string?> GetNameAsync(string accessToken, CancellationToken token);
}


// Example response from userinfo_endpoint
//{
//  "sub": "1238179238711321",
//  "name": "Jan Kowalski"
//}


// Example response from .well-known

//{
//  "issuer": "https://localhost:7068",
//  "jwks_uri": "https://localhost:7068/.well-known/openid-configuration/jwks",
//  "authorization_endpoint": "https://localhost:7068/connect/authorize",
//  "token_endpoint": "https://localhost:7068/connect/token",
//  "userinfo_endpoint": "https://localhost:7068/connect/userinfo",
//  "end_session_endpoint": "https://localhost:7068/connect/endsession",
//  "check_session_iframe": "https://localhost:7068/connect/checksession",
//  "revocation_endpoint": "https://localhost:7068/connect/revocation",
//  "introspection_endpoint": "https://localhost:7068/connect/introspect",
//  "device_authorization_endpoint": "https://localhost:7068/connect/deviceauthorization",
//  "frontchannel_logout_supported": true,
//  "frontchannel_logout_session_supported": true,
//  "backchannel_logout_supported": true,
//  "backchannel_logout_session_supported": true,
//  "scopes_supported": [
//    "openid",
//    "profile",
//    "email",
//    "tbdancedanceapi.read",
//    "tbdancedanceapi.write",
//    "tbdancedanceapi.convert",
//    "offline_access"
//  ],
//  "claims_supported": [
//    "sub",
//    "name",
//    "family_name",
//    "given_name",
//    "middle_name",
//    "nickname",
//    "preferred_username",
//    "profile",
//    "picture",
//    "website",
//    "gender",
//    "birthdate",
//    "zoneinfo",
//    "locale",
//    "updated_at",
//    "email",
//    "email_verified"
//  ],
//  "grant_types_supported": [
//    "authorization_code",
//    "client_credentials",
//    "refresh_token",
//    "implicit",
//    "password",
//    "urn:ietf:params:oauth:grant-type:device_code"
//  ],
//  "response_types_supported": [
//    "code",
//    "token",
//    "id_token",
//    "id_token token",
//    "code id_token",
//    "code token",
//    "code id_token token"
//  ],
//  "response_modes_supported": [
//    "form_post",
//    "query",
//    "fragment"
//  ],
//  "token_endpoint_auth_methods_supported": [
//    "client_secret_basic",
//    "client_secret_post"
//  ],
//  "id_token_signing_alg_values_supported": [
//    "RS256"
//  ],
//  "subject_types_supported": [
//    "public"
//  ],
//  "code_challenge_methods_supported": [
//    "plain",
//    "S256"
//  ],
//  "request_parameter_supported": true
//}