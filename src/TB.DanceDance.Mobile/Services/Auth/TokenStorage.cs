using IdentityModel.OidcClient;
using System.Diagnostics;
using System.Text.Json;

namespace TB.DanceDance.Mobile.Services.Auth;

static class TokenStorage
{
    public static SecurityToken? Token { get; private set; }
    private const string cache_key = "security_token";

    public static void SetToken(SecurityToken token)
    {
        Token = token;
    }
    
    public static async Task SaveRefreshTokenInStorage()
    {
        try
        {
            if (Token != null)
            {
                var json = JsonSerializer.Serialize(Token);
                await SecureStorage.Default.SetAsync(cache_key, json);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }


    public static async Task<string?> LoadRefreshTokenFromStorage()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(cache_key);
            if (json is not null)
            {
                Token = JsonSerializer.Deserialize<SecurityToken>(json);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }

        return Token?.RefreshToken;
    }
}