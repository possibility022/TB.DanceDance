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
            LoadProfile = false,
            BackchannelHandler = handler,
        };
        
        return options;
    }

    private OidcClientOptions GetClientOptionsForAndroid(HttpMessageHandler handler, string authority)
    {
        var options = GetBasicOptions(handler);
        // Keep authority aligned with issuer returned by the auth server metadata (localhost).
        // Network translation to 10.0.2.2 is handled by HTTP/browser layers for Android emulator.
        options.Authority = authority;
        var resolvedAuthority = networkAddressResolver.Resolve(authority);
        options.Policy.Discovery.AdditionalEndpointBaseAddresses.Add(resolvedAuthority);
        options.ClientId = AndroidClientId;
        options.RedirectUri = AndroidRedirectUri;
        return options;
    }
}
