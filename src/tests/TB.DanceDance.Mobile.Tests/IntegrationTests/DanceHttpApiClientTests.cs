using NSubstitute;
using System.Text.Json;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
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
    private readonly ITokenProviderService secondaryTokenProvider;

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
        secondaryTokenProvider = Substitute.For<ITokenProviderService>();

        server = WireMockServer.Start();
        var factory = new TestHttpClientFactory(server.Url!);
        client = new DanceHttpApiClient(factory, tokenProvider, secondaryTokenProvider);
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
        var obj = new UserEventsAndGroupsResponse
        {
            Assigned = new EventsAndGroups
            {
                Events = new List<Event>
                {
                    new Event { Id = Guid.NewGuid(), Name = "E", Date = DateTime.UtcNow }
                }
            }
        };

        // First call returns data
        server.Given(Request.Create().WithPath("/api/videos/accesses/my").UsingGet())
            .InScenario("useraccess")
            .WillSetStateTo("second")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(obj)));

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
        // default constructed object
        Assert.Empty(r2.Assigned.Events);
        Assert.Empty(r2.Assigned.Groups);
    }

    [Fact]
    public async Task RequestAccess_PostsSuccessfully()
    {
        server.Given(Request.Create().WithPath("/api/videos/accesses/request").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        await client.RequestAccess(new RequestAssigmentModelRequest { Events = new List<Guid> { Guid.NewGuid() } });

        var logs = server.FindLogEntries(Request.Create().WithPath("/api/videos/accesses/request").UsingPost());
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetVideosFromGroups_ReturnsCollection()
    {
        var payload = new[]
        {
            new GroupWithVideosResponse
            {
                GroupId = Guid.NewGuid(),
                GroupName = "G",
                Videos = new List<VideoInformationModel>
                {
                    new VideoInformationModel
                    {
                        Id = Guid.NewGuid(),
                        BlobId = "b",
                        Name = "n",
                        Converted = true,
                        RecordedDateTime = DateTime.UtcNow
                    }
                }
            }
        };
        server.Given(Request.Create().WithPath("/api/groups/videos").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload)));

        var res = await client.GetVideosFromGroups();
        Assert.NotNull(res);
        Assert.Single(res);
        Assert.Single(res.First().Videos);
    }

    [Fact]
    public async Task GetVideosForEvent_Works_NullBody_ReturnsEmpty_And_ThrowsOnError()
    {
        var eventId = Guid.NewGuid();
        var vids = new List<VideoInformationResponse>
        {
            new VideoInformationResponse
            {
                Id = Guid.NewGuid(),
                BlobId = "b1",
                Name = "v1",
                Converted = false,
                RecordedDateTime = DateTime.UtcNow
            }
        };

        // normal
        server.Given(Request.Create().WithPath($"/api/events/{eventId}/videos").UsingGet())
            .InScenario("videos")
            .WillSetStateTo("null")
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(vids)));

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
        var payload = new UploadVideoInformationResponse
        {
            Sas = server.Url + "/blob/sas", VideoId = id, ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        server.Given(Request.Create().WithPath($"/api/videos/upload/{id}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload)));

        var res = await client.RefreshUploadUrl(id);
        Assert.Equal(id, res.VideoId);
        Assert.Equal(payload.Sas, res.Sas);
    }

    [Fact]
    public async Task GetUploadInformation_PostsAndReturns()
    {
        var payload = new UploadVideoInformationResponse
        {
            Sas = server.Url + "/blob/vid", VideoId = Guid.NewGuid(), ExpireAt = DateTimeOffset.UtcNow.AddHours(1)
        };
        server.Given(Request.Create().WithPath("/api/videos/upload").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(payload)));

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
        var uri = client.GetVideoUri(blobId);
        Assert.StartsWith(server.Url, uri.ToString());
        Assert.Contains($"/api/videos/{blobId}/stream", uri.AbsolutePath);
        Assert.Contains("token=tok123", uri.Query);
    }

    public void Dispose()
    {
        server.Stop();
        server.Dispose();
    }
}