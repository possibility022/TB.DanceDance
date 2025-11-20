using Duende.IdentityModel.OidcClient;
using Microsoft.Maui.Devices;
using TB.DanceDance.Mobile.Services.Network;
using IBrowser = Duende.IdentityModel.OidcClient.Browser.IBrowser;

namespace TB.DanceDance.Mobile.Services.Auth;

public class AuthSettingsFactory
{
    private readonly IBrowser browser;
    private readonly DevicePlatform platform;
    private const string AndroidClientId = "tbdancedanceandroidapp";
    private const string AndroidRedirectUri = "tbdancedanceandroidapp://";
    
    public AuthSettingsFactory(IBrowser browser, DevicePlatform platform)
    {
        this.browser = browser;
        this.platform = platform;
    }
    
    public OidcClientOptions GetClientOptions(HttpMessageHandler httpClientHandler)
    {
        if (platform == DevicePlatform.Android)
            return GetClientOptionsForAndroid(httpClientHandler);

        return GetClientOptionsForAndroid(httpClientHandler);
        throw new PlatformNotSupportedException("This platform is not supported.");
    }

    private OidcClientOptions GetBasicOptions(HttpMessageHandler handler)
    {
        var options = new OidcClientOptions()
        {
            Browser = browser,
            Scope = "openid tbdancedanceapi.read offline_access profile",
            BackchannelHandler = handler,
        };
        
        return options;
    }

    private OidcClientOptions GetClientOptionsForAndroid(HttpMessageHandler handler)
    {
        var options = GetBasicOptions(handler);
        options.Authority = HttpClientFactory.ApiUrl;
        options.ClientId = AndroidClientId;
        options.RedirectUri = AndroidRedirectUri;
        return options;
    }
}