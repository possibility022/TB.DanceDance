using System.Net.Http.Headers;
using TB.DanceDance.Mobile.Library.Services.Auth;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class TokenDelegatingHandler : DelegatingHandler
{
    private KeyValuePair<(string, int), ITokenProviderService>? authorityA;
    private KeyValuePair<(string, int), ITokenProviderService>? authorityB;

    public TokenDelegatingHandler(ITokenProviderService primaryTokenProvider,
        ITokenProviderService secondaryTokenProvider)
    {
        this.authorityA = new KeyValuePair<(string, int), ITokenProviderService>(primaryTokenProvider.GetAuthority(), primaryTokenProvider);
        this.authorityB = new KeyValuePair<(string, int), ITokenProviderService>(secondaryTokenProvider.GetAuthority(), secondaryTokenProvider);
    }

    private ITokenProviderService GetOrSetTokenProviderForAuthority(string authorityHost, int authorityPort)
    {
        if (authorityA is not null
            && authorityA.Value.Key.Item1 == authorityHost
            && authorityA.Value.Key.Item2 == authorityPort)
            return authorityA.Value.Value;

        if (authorityB is not null
            && authorityB.Value.Key.Item1 == authorityHost
            && authorityB.Value.Key.Item2 == authorityPort)
            return authorityB.Value.Value;

        throw new InvalidOperationException("Something went wrong during cache initialization.");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null)
            throw new NotSupportedException("RequestUri cannot be null.");
        
        var provider = GetOrSetTokenProviderForAuthority(request.RequestUri.Host, request.RequestUri.Port);
        
        var token = await provider.GetAccessToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await base.SendAsync(request, cancellationToken);
        return res;
    }
}