using IdentityModel.OidcClient;
using Microsoft.Maui.Devices;
using System;
using System.Net.Http;

namespace TB.DanceDance.Mobile.Services.Auth;

public static class AuthSettingsFactory
{
#if DEBUG
    private const string AndroidAuthority = "https://10.0.2.2:7068";
#else
    private const string AndroidAuthority = "https://wcsdance.azurewebsites.net";
#endif
    
    private const string AndroidClientId = "tbdancedanceandroidapp";
    private const string AndroidRedirectUri = "tbdancedanceandroidapp://";
    
    public static OidcClientOptions GetClientOptions(HttpMessageHandler httpClientHandler)
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return GetClientOptionsForAndroid(httpClientHandler);

        return GetClientOptionsForAndroid(httpClientHandler);
        throw new PlatformNotSupportedException("This platform is not supported.");
    }

    private static OidcClientOptions GetBasicOptions(HttpMessageHandler handler)
    {
        var options = new OidcClientOptions()
        {
            Browser = new MauiAuthenticationBrowser(),
            Scope = "openid tbdancedanceapi.read offline_access profile",
            BackchannelHandler = handler,
        };
        
        return options;
    }

    private static OidcClientOptions GetClientOptionsForAndroid(HttpMessageHandler handler)
    {
        var options = GetBasicOptions(handler);
        options.Authority = AndroidAuthority;
        options.ClientId = AndroidClientId;
        options.RedirectUri = AndroidRedirectUri;
        return options;
    }
}