using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.Browser;
using TB.DanceDance.Mobile.Library.Services.Network;
using IBrowser = Duende.IdentityModel.OidcClient.Browser.IBrowser;

namespace TB.DanceDance.Mobile;

public class MauiAuthenticationBrowser : IBrowser
{
    private readonly NetworkAddressResolver networkAddressResolver;

    public MauiAuthenticationBrowser(NetworkAddressResolver networkAddressResolver)
    {
        this.networkAddressResolver = networkAddressResolver;
    }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var startUrl = networkAddressResolver.Resolve(new Uri(options.StartUrl));
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                startUrl,
                new Uri(options.EndUrl));

            var url = new RequestUrl("tbdancedanceandroidapp://")
                .Create(new Parameters(result.Properties));

            return new BrowserResult
            {
                Response = url,
                ResultType = BrowserResultType.Success,
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel
            };
        }
    }
}
