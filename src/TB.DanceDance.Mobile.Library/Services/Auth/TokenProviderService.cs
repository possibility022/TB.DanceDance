using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Results;
using Serilog;

namespace TB.DanceDance.Mobile.Library.Services.Auth;

public class TokenProviderService : ITokenProviderService
{
    private readonly OidcClient oidcClient;
    private readonly TokenStorage tokenStorage;
    private readonly (string, int) authority;

    public TokenProviderService(OidcClient oidcClient, TokenStorage tokenStorage)
    {
        this.oidcClient = oidcClient;
        this.tokenStorage = tokenStorage;

        var builder = new UriBuilder(oidcClient.Options.Authority);

        this.authority = (builder.Host, builder.Port);
    }

    private async Task<SecurityToken?> FetchAccessToken()
    {
        try
        {
            if (tokenStorage.Token?.RefreshToken is null)
                await tokenStorage.LoadRefreshTokenFromStorage();

            if (tokenStorage.Token?.RefreshToken is not null)
            {
                RefreshTokenResult? refreshResults = await oidcClient.RefreshTokenAsync(tokenStorage.Token.RefreshToken);
                if (refreshResults is not null && !refreshResults.IsError)
                {
                    tokenStorage.SetToken(new SecurityToken()
                    {
                        RefreshToken = refreshResults.RefreshToken,
                        AccessToken = refreshResults.AccessToken,
                        IdentityToken = refreshResults.IdentityToken,
                        AccessTokenExpiration = refreshResults.AccessTokenExpiration
                    });

                    await tokenStorage.SaveRefreshTokenInStorage();

                    return tokenStorage.Token;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not get token silently");
        }

        var response = await oidcClient.LoginAsync();

        if (response?.IsError == false)
        {
            tokenStorage.SetToken(new SecurityToken()
            {
                RefreshToken = response.RefreshToken,
                AccessToken = response.AccessToken,
                IdentityToken = response.IdentityToken,
                AccessTokenExpiration = response.AccessTokenExpiration
            });
            
            await tokenStorage.SaveRefreshTokenInStorage();
        }

        return tokenStorage.Token;
    }

    /// <summary>
    /// Returns valid access token. Starts authentication flow or refresh is token expired or is not available.
    /// </summary>
    /// <returns>Valid access token or null if login failed.</returns>
    public async Task<string?> GetAccessToken()
    {
        if (tokenStorage.Token?.AccessToken == null 
            || tokenStorage.Token.AccessTokenExpiration < DateTimeOffset.Now.AddMinutes(-5))
        {
            var token = await FetchAccessToken();
            return token?.AccessToken;
        }

        return tokenStorage.Token.AccessToken;
    }

    public string? GetValidAccessTokenNoFetch()
    {
        if (tokenStorage.Token?.AccessToken == null
            || tokenStorage.Token.AccessTokenExpiration < DateTimeOffset.Now.AddMinutes(-1))
            return null;
        
        return tokenStorage.Token.AccessToken;
    }

    public (string, int) GetAuthority() => authority;
}

public interface ITokenProviderService
{
    public Task<string?> GetAccessToken();

    public string? GetValidAccessTokenNoFetch();

    public (string, int) GetAuthority();
}