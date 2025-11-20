using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Results;

namespace TB.DanceDance.Mobile.Library.Services.Auth;

public class TokenProviderService : ITokenProviderService
{
    private readonly OidcClient oidcClient;

    public TokenProviderService(OidcClient oidcClient)
    {
        this.oidcClient = oidcClient;
    }

    private async Task<SecurityToken?> FetchAccessToken()
    {
        if (TokenStorage.Token?.RefreshToken is null)
            await TokenStorage.LoadRefreshTokenFromStorage();
        
        if (TokenStorage.Token?.RefreshToken is not null)
        {
            RefreshTokenResult? refreshResults = await oidcClient.RefreshTokenAsync(TokenStorage.Token.RefreshToken);
            if (refreshResults is not null && !refreshResults.IsError)
            {
                TokenStorage.SetToken(new SecurityToken()
                {
                    RefreshToken = refreshResults.RefreshToken,
                    AccessToken = refreshResults.AccessToken,
                    IdentityToken = refreshResults.IdentityToken,
                    AccessTokenExpiration = refreshResults.AccessTokenExpiration
                });

                await TokenStorage.SaveRefreshTokenInStorage();
                
                return TokenStorage.Token;
            }
        }

        var response = await oidcClient.LoginAsync();

        if (response?.IsError == false)
        {
            TokenStorage.SetToken(new SecurityToken()
            {
                RefreshToken = response.RefreshToken,
                AccessToken = response.AccessToken,
                IdentityToken = response.IdentityToken,
                AccessTokenExpiration = response.AccessTokenExpiration
            });
            
            await TokenStorage.SaveRefreshTokenInStorage();
        }

        return TokenStorage.Token;
    }

    /// <summary>
    /// Returns valid access token. Starts authentication flow or refresh is token expired or is not available.
    /// </summary>
    /// <returns>Valid access token or null if login failed.</returns>
    public async Task<string?> GetAccessToken()
    {
        if (TokenStorage.Token?.AccessToken == null 
            || TokenStorage.Token.AccessTokenExpiration < DateTimeOffset.Now.AddMinutes(-5))
        {
            var token = await FetchAccessToken();
            return token?.AccessToken;
        }

        return TokenStorage.Token.AccessToken;
    }
}

public interface ITokenProviderService
{
    public Task<string?> GetAccessToken();
}