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
    WireMockServer server;
    private readonly ITokenProviderService tokenProvider;
    private HttpClient httpClient = null!;

    public TokenDelegatingHandlerTests()
    {
        server = WireMockServer.Start();
        
        tokenProvider = Substitute.For<ITokenProviderService>();
        tokenProvider.GetAccessToken().Returns("access_token");
    }

    private void CreateHandlerForTests(HttpMessageHandler? messageHandler = null)
    {
        // this, to replicate MAUI setup. The difference is in retry.
        var rp = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new HttpRetryStrategyOptions { BackoffType = DelayBackoffType.Exponential, MaxRetryAttempts = 1 })
            .Build();

        messageHandler ??= new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(15) };

        var handler = new TokenDelegatingHandler(tokenProvider, rp)
        {
            InnerHandler = messageHandler
        };
        
        httpClient = new HttpClient(handler);
    }

    [Fact]
    public async Task TokenDelegatingHandler_When500_CallOnErrorAction()
    {
        server.Given(Request.Create()
                .UsingAnyMethod())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody(string.Empty));

        int calls = 0;
        
        CreateHandlerForTests();
        
        var res = await httpClient.GetAsync(server.Urls[0], TestContext.Current.CancellationToken);
        
        Assert.Equal(1, calls);
    }
    
    [Fact]
    public async Task TokenDelegatingHandler_WhenException_CallOnErrorAction()
    {
        int calls = 0;
        CreateHandlerForTests(new ThrowingHandler());
        
        var res = await Assert.ThrowsAsync<Exception>(() => httpClient.GetAsync(server.Urls[0], TestContext.Current.CancellationToken));
        
        Assert.Equal("Test Exception",res.Message);
        Assert.Equal(1, calls);
    }

    class ThrowingHandler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new Exception("Test Exception");
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new Exception("Test Exception");
        }
    }

    public void Dispose()
    {
        server.Dispose();
        httpClient.Dispose();
    }
}