using Duende.IdentityModel.OidcClient;
using Microsoft.Maui.Devices;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class AuthSettingsFactory
{
    private readonly IBrowserFactory browserFactory;
    private readonly NetworkAddressResolver networkAddressResolver;
    private readonly DevicePlatform platform;
    private const string AndroidClientId = "tbdancedanceandroidapp";
    private const string AndroidRedirectUri = "tbdancedanceandroidapp://";
    
    public AuthSettingsFactory(IBrowserFactory browserFactory, NetworkAddressResolver networkAddressResolver, DevicePlatform platform)
    {
        this.browserFactory = browserFactory;
        this.networkAddressResolver = networkAddressResolver;
        this.platform = platform;
    }
    
    public OidcClientOptions GetClientOptions(HttpMessageHandler httpClientHandler, string authority)
    {
        if (platform == DevicePlatform.Android)
            return GetClientOptionsForAndroid(httpClientHandler, authority);

        return GetClientOptionsForAndroid(httpClientHandler, authority);
        throw new PlatformNotSupportedException("This platform is not supported.");
    }

    private OidcClientOptions GetBasicOptions(HttpMessageHandler handler)
    {
        var options = new OidcClientOptions()
        {
            Browser = browserFactory.CreateBrowser(),
            Scope = "openid tbdancedanceapi.read offline_access profile",
            BackchannelHandler = handler,
        };
        
        return options;
    }

    private OidcClientOptions GetClientOptionsForAndroid(HttpMessageHandler handler, string authority)
    {
        var options = GetBasicOptions(handler);
        options.Authority = networkAddressResolver.Resolve(authority);
        options.ClientId = AndroidClientId;
        options.RedirectUri = AndroidRedirectUri;
        return options;
    }
}