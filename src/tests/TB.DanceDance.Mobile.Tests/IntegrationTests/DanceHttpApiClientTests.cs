using NSubstitute;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Contracts.Features.Events.Models;
using TB.DanceDance.API.Contracts.Features.Groups;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Sharing;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class DanceHttpApiClientTests : IDisposable
{
    private readonly WireMockServer server;
    private readonly DanceHttpApiClient client;
    private readonly ITokenProviderService tokenProvider;
    private readonly JsonSerializerOptions serializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Uri baseAddress;

        public TestHttpClientFactory(string baseUrl)
        {
            baseAddress = new Uri(baseUrl);
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient { BaseAddress = baseAddress };
        }
    }

    public DanceHttpApiClientTests()
    {
        tokenProvider = Substitute.For<ITokenProviderService>();

        server = WireMockServer.Start();
        var factory = new TestHttpClientFactory(server.Url!);
        client = new DanceHttpApiClient(factory, tokenProvider);
    }

    [Fact]
    public async Task RenameVideoAsync_PostsSuccessfully()
    {
        var id = Guid.NewGuid();
        server.Given(Request.Create().WithPath($"/api/videos/{id}/rename").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        await client.RenameVideoAsync(id, "new-name");

        var logs = server.FindLogEntries(Request.Create().WithPath($"/api/videos/{id}/rename").UsingPost());
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetUserAccesses_ReturnsContent_OrEmptyOnNull()
    {
        var obj = new GetUserAccessResponse
        {
            Assigned = new GetUserAccessSet
            {
                Events = new List<EventModel>
                {
                    new EventModel { Id = Guid.NewGuid(), Name = "E", Date = DateTime.UtcNow }
                }
            },
            Available = new GetUserAccessSet(),
            Pending   = new ListUserAccessPending()
        };

        // First call returns data
        server.Given(Request.Create().WithPath("/api/videos/accesses/my").UsingGet())
            .InScenario("useraccess")
            .WillSetStateTo("second")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(obj, serializerOptions)));

        // Second call returns null body
        server.Given(Request.Create().WithPath("/api/videos/accesses/my").UsingGet())
            .InScenario("useraccess")
            .WhenStateIs("second")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        var r1 = await client.GetUserAccesses();
        Assert.NotNull(r1);
        Assert.NotEmpty(r1.Assigned.Events);

        var r2 = await client.GetUserAccesses();
        Assert.NotNull(r2);
        Assert.Empty(r2.Assigned.Events);
        Assert.Empty(r2.Assigned.Groups);
    }

    [Fact]
    public async Task RequestAccess_PostsSuccessfully()
    {
        server.Given(Request.Create().WithPath("/api/videos/accesses/request").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(204));

        await client.RequestAccess(new RequestAccessRequest { Events = new List<Guid> { Guid.NewGuid() } });

        var logs = server.FindLogEntries(Request.Create().WithPath("/api/videos/accesses/request").UsingPost());
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetVideosFromGroups_ReturnsCollection()
    {
        var payload = new ListGroupVideosResponse
        {
            Videos = new[]
            {
                new VideoFromGroupInformation
                {
                    VideoId = Guid.NewGuid(),
                    GroupId = Guid.NewGuid(),
                    GroupName = "G",
                    BlobId = "b",
                    Name = "n",
                    Converted = true,
                    RecordedDateTime = DateTime.UtcNow
                }
            }
        };
        server.Given(Request.Create().WithPath("/api/groups/videos").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, serializerOptions)));

        var res = await client.GetVideosFromGroups();
        Assert.NotNull(res);
        Assert.Single(res);
        Assert.Equal("G", res.First().GroupName);
    }

    [Fact]
    public async Task GetVideosForEvent_Works_NullBody_ReturnsEmpty_And_ThrowsOnError()
    {
        var eventId = Guid.NewGuid();
        var vids = new ListEventVideosResponse
        {
            Videos = new List<VideoInformation>
            {
                new VideoInformation
                {
                    VideoId = Guid.NewGuid(),
                    BlobId = "b1",
                    Name = "v1",
                    Converted = false,
                    RecordedDateTime = DateTime.UtcNow
                }
            }
        };

        // normal
        server.Given(Request.Create().WithPath($"/api/events/{eventId}/videos").UsingGet())
            .InScenario("videos")
            .WillSetStateTo("null")
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(vids, serializerOptions)));

        var list = await client.GetVideosForEvent(eventId);
        Assert.Single(list);

        // null body
        server.Given(Request.Create().WithPath($"/api/events/{eventId}/videos").UsingGet())
            .InScenario("videos").WhenStateIs("null")
            .WillSetStateTo("error")
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        var empty = await client.GetVideosForEvent(eventId);
        Assert.Empty(empty);

        // error
        server.Given(Request.Create().WithPath($"/api/events/{eventId}/videos").UsingGet())
            .InScenario("videos").WhenStateIs("error")
            .RespondWith(Response.Create().WithStatusCode(500));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetVideosForEvent(eventId));
    }

    [Fact]
    public async Task RefreshUploadUrl_ReturnsData()
    {
        var id = Guid.NewGuid();
        var payload = new RefreshUploadUrlResponse
        {
            Sas = server.Url + "/blob/sas", VideoId = id, ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        server.Given(Request.Create().WithPath($"/api/videos/upload/{id}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, serializerOptions)));

        var res = await client.RefreshUploadUrl(id);
        Assert.Equal(id, res.VideoId);
        Assert.Equal(payload.Sas, res.Sas);
    }

    [Fact]
    public async Task GetUploadInformation_PostsAndReturns()
    {
        var payload = new ProduceUploadUrlResponse
        {
            Sas = server.Url + "/blob/vid", VideoId = Guid.NewGuid(), ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        server.Given(Request.Create().WithPath("/api/videos/upload").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, serializerOptions)));

        var res = await client.GetUploadInformation("file.mp4", "Nice name", SharingWithType.Event, Guid.NewGuid(),
            DateTime.UtcNow);
        Assert.NotNull(res);
        Assert.Equal(payload.Sas, res.Sas);
    }

    [Fact]
    public async Task GetStream_StreamsContent()
    {
        var blobId = "abc";
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        server.Given(Request.Create().WithPath($"/api/videos/{blobId}/stream").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/octet-stream")
                .WithBody(bytes));

        await using var stream = await client.GetStream(blobId);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public void GetVideoUri_IncludesAccessTokenQuery()
    {
        tokenProvider.GetValidAccessTokenNoFetch()
            .Returns("tok123");

        var blobId = "xyz";
        (var uri, var token) = client.GetVideoUri(blobId);
        Assert.StartsWith(server.Url, uri.ToString());
        Assert.Contains($"/api/videos/{blobId}/stream", uri.AbsolutePath);
        Assert.Equal("tok123", token);
    }

    [Fact]
    public async Task GetMyVideos_Returns()
    {
        var item = new VideoInformation
        {
            VideoId          = Guid.NewGuid(),
            BlobId           = Guid.NewGuid().ToString(),
            Converted        = false,
            Duration         = TimeSpan.FromHours(1),
            Name             = "Some Name",
            RecordedDateTime = DateTime.UtcNow,
        };
        var payload = new MyVideosResponse
        {
            VideoInformation = new[] { item }
        };

        server.Given(Request.Create().WithPath("/api/videos/my").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload, serializerOptions)));

        var res = await client.GetMyVideos();

        Assert.NotNull(res);
        Assert.Equal(item.BlobId, res.First().BlobId);
        Assert.Equal(item.Converted, res.First().Converted);
        Assert.Equal(item.Duration, res.First().Duration);
        Assert.Equal(item.VideoId, res.First().VideoId);
        Assert.Equal(item.RecordedDateTime, res.First().RecordedDateTime);
    }

    [Fact]
    public async Task GetSharingLinkAsync_PostsAndReturns()
    {
        CreateSharedLinkRequest requestPayload = new() { ExpirationDays = 7 };
        SharedLinkResponse responsePayload = new SharedLinkResponse()
        {
            CreatedAt = DateTimeOffset.UtcNow,
            ExpireAt = DateTimeOffset.UtcNow.AddHours(1),
            IsRevoked = false,
            LinkId = Guid.NewGuid().ToString(),
            ShareUrl = "https://example.com/share",
            VideoId = Guid.NewGuid(),
            VideoName = Guid.NewGuid().ToString(),
        };

        var videoId = Guid.NewGuid();

        server.Given(
                Request
                    .Create()
                    .WithPath($"/api/videos/{videoId}/share")
                    .UsingPost()
                    .WithBodyAsJson(JsonSerializer.Serialize(requestPayload, serializerOptions)))
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(responsePayload, serializerOptions)));

        var res = await client.GetSharingLinkAsync(videoId, TestContext.Current.CancellationToken);

        Assert.NotNull(res);
        Assert.Equal(responsePayload.CreatedAt, res.CreatedAt);
        Assert.Equal(responsePayload.ExpireAt, res.ExpireAt);
        Assert.Equal(responsePayload.IsRevoked, res.IsRevoked);
        Assert.Equal(responsePayload.LinkId, res.LinkId);
        Assert.Equal(responsePayload.ShareUrl, res.ShareUrl);
        Assert.Equal(responsePayload.VideoId, res.VideoId);
        Assert.Equal(responsePayload.VideoName, res.VideoName);
    }

    [Fact]
    public async Task RevokeSharingLinkAsync_Deletes()
    {
        var linkId = Guid.NewGuid();

        server.Given(
                Request
                    .Create()
                    .WithPath("/api/share/" + linkId)
                    .UsingDelete())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(204)
                );

        await client.RevokeShareLinkAsync(linkId.ToString(), TestContext.Current.CancellationToken);

        // No assertion. If not 2xx, should throw
    }

    public void Dispose()
    {
        server.Stop();
        server.Dispose();
    }
}
