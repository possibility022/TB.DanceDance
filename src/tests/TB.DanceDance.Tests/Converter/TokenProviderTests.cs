using System.Text;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TB.DanceDance.Tests.Converter;

public class TokenProviderTests : IDisposable
{
    private readonly WireMockServer wireMockServer;
    private readonly TokenProvider tokenProvider;
    private readonly OAuthHttpClient oAuthHttpClient;
    
    public TokenProviderTests()
    {
        wireMockServer = WireMockServer.Start();
        oAuthHttpClient = new OAuthHttpClient() { BaseAddress = new Uri(wireMockServer.Url!) };
        tokenProvider = new TokenProvider(oAuthHttpClient,
            new TokenProviderOptions()
            {
                ClientId = "client_id", ClientSecret = "client_secret", Scope = "client_scope"
            });
    }

    [Fact]
    public async Task GetTokenAsync_SendsCorrectRequest_ParsesResponse()
    {
        var tokenJson = "{  \"access_token\": \"abc123\",  \"expires_in\": 3600,  \"token_type\": \"Bearer\"}";

        wireMockServer
            .Given(Request.Create()
                .WithPath("/connect/token")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(tokenJson));

        var token = await tokenProvider.GetTokenAsync(CancellationToken.None);

        Assert.Equal("abc123", token.AccessToken);
        Assert.Equal("Bearer", token.Schema);

        var log = wireMockServer.FindLogEntries(Request.Create().WithPath("/connect/token").UsingPost());
        Assert.Single(log);

        var requestMessage = log[0].RequestMessage;
        Assert.Equal("/connect/token", requestMessage.Path);
        Assert.Equal("POST", requestMessage.Method);

        // Authorization header should be Basic base64(client_id:client_secret)
        var expectedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes("client_id:client_secret"));
        Assert.True(requestMessage.Headers!.TryGetValue("Authorization", out var authHeader));
        Assert.Contains($"Basic {expectedAuth}", authHeader);

        // Body should contain form fields
        var bodyString = requestMessage.Body;
        Assert.Contains("grant_type=client_credentials", bodyString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope=client_scope", bodyString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTokenAsync_CachesToken_UntilExpiry()
    {
        var tokenJson = "{  \"access_token\": \"cached_token\",  \"expires_in\": 3600,  \"token_type\": \"Bearer\"}";

        wireMockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(tokenJson));

        var t1 = await tokenProvider.GetTokenAsync(CancellationToken.None);
        var t2 = await tokenProvider.GetTokenAsync(CancellationToken.None);

        Assert.Same(t1, t2); // cached instance should be returned

        var logs = wireMockServer.FindLogEntries(Request.Create().WithPath("/connect/token").UsingPost());
        Assert.Single(logs); // only one HTTP call should be made
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsOnNonSuccessStatus()
    {
        wireMockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));

        await Assert.ThrowsAsync<HttpRequestException>(() => tokenProvider.GetTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsWhenContentIsNull()
    {
        wireMockServer
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        await Assert.ThrowsAsync<NullReferenceException>(() => tokenProvider.GetTokenAsync(CancellationToken.None));
    }


    public void Dispose()
    {
        oAuthHttpClient.Dispose();
        wireMockServer.Dispose();
    }
}