using Microsoft.Maui.Storage;
using System.Text.Json;

namespace TB.DanceDance.Mobile.Library.Services.Auth;

public class TokenStorage
{
    public const string PrimaryStorageKey = "PrimaryTokenStorage";
    public const string SecondaryStorageKey = "SecondaryTokenStorage";
    
    public SecurityToken? Token { get; private set; }
    private readonly string cacheKey;

    public TokenStorage(string cacheKey)
    {
        this.cacheKey = $"access_token_{cacheKey}";
    }

    public void SetToken(SecurityToken token)
    {
        Token = token;
    }

    public void ClearToken()
    {
        Token = null;
        SecureStorage.Default.SetAsync(cacheKey, string.Empty);
    }
    
    public async Task SaveRefreshTokenInStorage()
    {
        try
        {
            if (Token != null)
            {
                var json = JsonSerializer.Serialize(Token);
                await SecureStorage.Default.SetAsync(cacheKey, json);
            }
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "Error during saving token into secure storage.");
        }
    }


    public async Task<string?> LoadRefreshTokenFromStorage()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(cacheKey);
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