using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;
using Microsoft.Maui.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;

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