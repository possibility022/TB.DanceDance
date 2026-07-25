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

    [Fact]
    public async Task SendAsync_BackgroundScope_NeverStartsInteractiveLogin()
    {
        tokenProvider.GetAccessTokenSilently().Returns((string?)null);
        var handler = new TokenDelegatingHandler(tokenProvider)
        {
            InnerHandler = new HttpClientHandler()
        };
        httpClient = new HttpClient(handler);
        using var scope = BackgroundAuthenticationContext.RequireSilentAuthentication();

        await Assert.ThrowsAsync<BackgroundAuthenticationRequiredException>(
            () => httpClient.GetAsync(server.Url!, TestContext.Current.CancellationToken));

        await tokenProvider.Received(1).GetAccessTokenSilently();
        await tokenProvider.DidNotReceive().GetAccessToken();
    }

    public void Dispose()
    {
        server.Dispose();
        httpClient.Dispose();
    }
}