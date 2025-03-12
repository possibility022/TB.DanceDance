using IdentityModel.OidcClient;
using System;
using System.Threading.Tasks;

namespace TB.DanceDance.Mobile.Services.Auth;

public class TokenProviderService : ITokenProviderService
{
    private readonly OidcClient oidcClient;

    public TokenProviderService(OidcClient oidcClient)
    {
        this.oidcClient = oidcClient;
    }

    private async Task<LoginResult?> FetchAccessToken()
    {
        var response = await oidcClient.LoginAsync();

        if (response != null)
            TokenStorage.LoginResult = response;

        return response;
    }

    /// <summary>
    /// Returns valid access token. Starts authentication flow or refresh is token expired or is not available.
    /// </summary>
    /// <returns>Valid access token or null if login failed.</returns>
    public async Task<string?> GetAccessToken()
    {
        if (TokenStorage.LoginResult == null 
            || TokenStorage.LoginResult.AccessTokenExpiration > DateTimeOffset.Now.AddMinutes(-5))
        {
            var token = await FetchAccessToken();
            return token?.AccessToken;
        }

        return TokenStorage.LoginResult.AccessToken;
    }
}

public interface ITokenProviderService
{
    public Task<string?> GetAccessToken();
}