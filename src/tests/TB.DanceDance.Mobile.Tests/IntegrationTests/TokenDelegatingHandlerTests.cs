using Microsoft.Extensions.Http.Resilience;
using NSubstitute;
using Polly;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.Network;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class TokenDelegatingHandlerTests : IDisposable
{
    private readonly WireMockServer server;
    private readonly ITokenProviderService tokenProvider;
    private readonly ITokenProviderService secondaryTokenProvider;
    private HttpClient httpClient = null!;

    public TokenDelegatingHandlerTests()
    {
        server = WireMockServer.Start();
        
        tokenProvider = Substitute.For<ITokenProviderService>();
        tokenProvider.GetAccessToken().Returns("access_token");
        
        secondaryTokenProvider = Substitute.For<ITokenProviderService>();
        secondaryTokenProvider.GetAccessToken().Returns("access_token");
    }

    public void Dispose()
    {
        server.Dispose();
        httpClient.Dispose();
    }
}