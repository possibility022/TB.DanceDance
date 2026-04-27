using System.Net.Http.Headers;
using TB.DanceDance.Mobile.Library.Services.Auth;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class TokenDelegatingHandler : DelegatingHandler
{
    private readonly KeyValuePair<(string, int), ITokenProviderService>? authorityA;

    public TokenDelegatingHandler(ITokenProviderService primaryTokenProvider)
    {
        this.authorityA = new KeyValuePair<(string, int), ITokenProviderService>(primaryTokenProvider.GetAuthority(), primaryTokenProvider);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            throw new NotSupportedException("RequestUri cannot be null.");
        
        var provider = authorityA!.Value.Value;
        
        var token = await provider.GetAccessToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await base.SendAsync(request, cancellationToken);
        return res;
    }
}