using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient.Browser;
using IBrowser = Duende.IdentityModel.OidcClient.Browser.IBrowser;

namespace TB.DanceDance.Mobile.Services.Auth;

public class MauiAuthenticationBrowser : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new Uri(options.StartUrl),
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