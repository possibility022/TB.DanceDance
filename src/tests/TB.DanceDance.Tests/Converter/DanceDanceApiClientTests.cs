using TB.DanceDance.Services.Converter.Deamon;
using TB.DanceDance.Services.Converter.Deamon.OAuthClient;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using TB.DanceDance.API.Contracts.Features.Videos.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TB.DanceDance.Tests.Converter;

public class DanceDanceApiClientTests : IDisposable
{
    readonly WireMockServer server;
    private readonly DanceDanceApiClient danceApiClient;
    private readonly ApiHttpClient apiHttpClient;
    private readonly TokenHttpHandler tokenHttpHandler;
    private readonly OAuthHttpClient oAuthHttpClient;
    private readonly HttpClient blobHttpClient;

    public DanceDanceApiClientTests()
    {
        server = WireMockServer.Start();
        oAuthHttpClient = new OAuthHttpClient()
        {
            BaseAddress = new Uri(server.Urls[0])
        };
        
        tokenHttpHandler = new TokenHttpHandler(
            new TokenProvider(oAuthHttpClient,
                new TokenProviderOptions()
                {
                    Scope = "scope", ClientId = "client_id", ClientSecret = "client_secret",
                }));
        
        apiHttpClient = new ApiHttpClient(tokenHttpHandler)
        {
            Timeout = TimeSpan.FromSeconds(60 * 5),
            BaseAddress = new Uri(server.Urls[0])
        };
        
        blobHttpClient = new HttpClient();
        danceApiClient = new DanceDanceApiClient(apiHttpClient, blobHttpClient, new ProgramConfig());
    }

    private void StubToken()
    {
        var tokenJson = "{\n  \"access_token\": \"abc123\",\n  \"expires_in\": 3600,\n  \"token_type\": \"Bearer\"\n}";
        server
            .Given(Request.Create().WithPath("/connect/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(tokenJson));
    }

    [Fact]
    public async Task GetNextVideoToConvertAsync_ReturnsObject_On200()
    {
        StubToken();
        var videoToTransformModel = new VideoToTransformModel
        {
            Id = Guid.NewGuid(),
            FileName = "video.mp4",
            Sas = server.Url + "/blob/video.mp4"
        };

        var content = new VideoToTransformResponse() { VideoExists = true, VideoToTransform = videoToTransformModel };

        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        server
            .Given(Request.Create().WithPath("/api/converter/videos").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(json));

        var res = await danceApiClient.GetNextVideoToConvertAsync(CancellationToken.None);
        Assert.NotNull(res);
        Assert.Equal(videoToTransformModel.Id, res.VideoToTransform!.Id);
        Assert.Equal(videoToTransformModel.FileName, res.VideoToTransform!.FileName);
        Assert.Equal(videoToTransformModel.Sas, res.VideoToTransform!.Sas);
    }

    [Fact]
    public async Task GetNextVideoToConvertAsync_Throws_OnNonSuccess()
    {
        StubToken();
        server
            .Given(Request.Create().WithPath("/api/converter/videos").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        await Assert.ThrowsAsync<HttpRequestException>(() => danceApiClient.GetNextVideoToConvertAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetNextVideoToConvertAsync_Throws_OnNullBody()
    {
        StubToken();
        server
            .Given(Request.Create().WithPath("/api/converter/videos").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        await Assert.ThrowsAsync<NullReferenceException>(() => danceApiClient.GetNextVideoToConvertAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetVideoToConvertAsync_StreamsContent()
    {
        // Arrange
        StubToken();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var blobPath = "/blob/video.bin";
        server
            .Given(Request.Create().WithPath(blobPath).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(bytes)
                .WithHeader("Content-Type", "application/octet-stream"));

        using var target = new MemoryStream();
        var url = new Uri(server.Url + blobPath);

        // Act
        await danceApiClient.GetVideoToConvertAsync(target, url, CancellationToken.None);

        // Assert
        Assert.Equal(bytes, target.ToArray());
    }

    [Fact]
    public async Task UploadVideoToTransformInformation_PostsSuccessfully()
    {
        StubToken();
        server
            .Given(Request.Create().WithPath("/api/converter/videos").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var req = new UpdateVideoInfoRequest
        {
            VideoId = Guid.NewGuid(),
            RecordedDateTime = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(2),
            Metadata = new byte[] { 10, 20, 30 }
        };

        await danceApiClient.UploadVideoToTransformInformation(req, CancellationToken.None);

        var logs = server.FindLogEntries(Request.Create().WithPath("/api/converter/videos").UsingPost());
        Assert.Single(logs);
    }

    [Fact]
    public async Task PublishTransformedVideo_PostsSuccessfully()
    {
        StubToken();
        var id = Guid.NewGuid();
        server
            .Given(Request.Create().WithPath($"/api/converter/videos/{id}/publish").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        await danceApiClient.PublishTransformedVideo(id, CancellationToken.None);

        var logs = server.FindLogEntries(Request.Create().WithPath($"/api/converter/videos/{id}/publish").UsingPost());
        Assert.Single(logs);
    }

    [Fact(Skip = "Requires Azure Blob SDK interaction over SAS; out of scope for unit tests")]
    public async Task UploadContent_Skipped()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        apiHttpClient.Dispose();
        blobHttpClient.Dispose();
        oAuthHttpClient.Dispose();
        server.Stop();
        server.Dispose();
    }
}