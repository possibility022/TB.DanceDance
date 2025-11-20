using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Net.Http.Headers;
using TB.DanceDance.Mobile.Library.Services.Auth;

namespace TB.DanceDance.Mobile.Library.Services.Network;

public class TokenDelegatingHandler : ResilienceHandler
{
    private readonly ITokenProviderService tokenProvider;    

    public TokenDelegatingHandler(ITokenProviderService tokenProvider, ResiliencePipeline<HttpResponseMessage> resiliencePipeline) : base(resiliencePipeline)
    {
        this.tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) 
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokenProvider.GetAccessToken());
        return await base.SendAsync(request, cancellationToken);
    }
}