using Microsoft.Maui.Storage;
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

    public static void ClearToken()
    {
        Token = null;
        SecureStorage.Default.SetAsync(cache_key, string.Empty);
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
            Serilog.Log.Error(e, "Error during saving token into secure storage.");
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
            Serilog.Log.Error(e, "Error during loading refresh token from secure storage.");
        }

        return Token?.RefreshToken;
    }
}