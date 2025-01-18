using IdentityModel.OidcClient;

namespace TB.DanceDance.Mobile.Services.Configuration;

public static class AuthSettingsFactory
{
    #if DEBUG
    private const string AndroidAuthority = "https://10.0.2.2:7068";
    private const string AndroidClientId = "tbdancedanceandroidapp";
    #else
    private const string AndroidAuthority = "https://";
    private const string AndroidClientId = "tbdancedanceandroidapp";
    #endif
    
    public static OidcClientOptions GetClientOptions(HttpClientHandler httpClientHandler)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return GetClientOptionsForAndroid(httpClientHandler);

        throw new PlatformNotSupportedException("This platform is not supported.");
    }

    private static OidcClientOptions GetBasicOptions(HttpClientHandler handler)
    {
        var options = new OidcClientOptions()
        {
            Browser = new MauiAuthenticationBrowser(),
            Scope = "openid tbdancedanceapi.read offline_access profile",
            BackchannelHandler = handler,
        };
        
        return options;
    }

    private static OidcClientOptions GetClientOptionsForAndroid(HttpClientHandler handler)
    {
        var options = GetBasicOptions(handler);
        options.Authority = AndroidAuthority;
        options.ClientId = AndroidClientId;
        return options;
    }
}