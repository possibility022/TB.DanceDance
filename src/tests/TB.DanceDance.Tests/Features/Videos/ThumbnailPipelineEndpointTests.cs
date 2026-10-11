using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application;
using Domain;
using FastEndpoints.Testing;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Videos;

/// <summary>
/// End-to-end coverage of the thumbnail pipeline through the real API host (Testcontainers
/// Postgres + Azurite, real FastEndpoints auth policies). Service-level behavior already
/// covered by <see cref="VideoUploaderTests"/> (publish/listing) and
/// <see cref="AccessManagement.AccessServiceTests"/> (sharing rules) is not repeated here.
/// </summary>
public class ThumbnailPipelineEndpointTests(WebAppFixture App) : TestBaseWithAssemblyFixture<WebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string ThumbnailSasRoute(Guid videoId) =>
        ApiRoutes.Converter.GetThumbnailSas.Replace("{videoId}", videoId.ToString());

    private static string PublishThumbnailRoute(Guid videoId) =>
        ApiRoutes.Converter.PublishThumbnail.Replace("{videoId}", videoId.ToString());

    private static string VideoInfoRoute(string blobId) =>
        ApiRoutes.Video.GetSingle.Replace("{blobId}", blobId);

    /// <summary>
    /// WebAppFixture's Postgres container is shared across every test in this class (xunit runs
    /// them sequentially), so leftover rows from earlier tests could otherwise win the
    /// "next video for thumbnail" pick instead of the one this test just created.
    /// </summary>
    private async Task MakeAllExistingVideosIneligibleForThumbnail()
    {
        await using var db = App.CreateDbContext();
        var existing = await db.Videos.ToListAsync(Cancellation);
        if (existing.Count == 0)
            return;

        foreach (var v in existing)
            v.LockedTill = DateTime.UtcNow.AddDays(365 * 10);
        await db.SaveChangesAsync(Cancellation);
    }

    [Fact]
    public async Task GetThumbnails_WithConvertScope_ReturnsOk()
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);

        var response = await client.GetAsync(ApiRoutes.Converter.Thumbnails, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("?limit=3", 3)]
    [InlineData("?limit=20", 12)]
    public async Task GetVideosMissingThumbnails_RespectsLimit_AndReturnsSourceIds(string query, int expectedCount)
    {
        await MakeAllExistingVideosIneligibleForThumbnail();
        var owner = new UserDataBuilder().Build();
        var now = DateTime.UtcNow;
        var videos = Enumerable.Range(0, 12)
            .Select(i => new VideoDataBuilder().OwnedBy(owner).Converted()
                .SharedAt(now.AddMinutes(-i)).Build())
            .ToArray();

        await using var db = App.CreateDbContext();
        db.Add(owner);
        db.AddRange(videos);
        await db.SaveChangesAsync(Cancellation);
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);

        var response = await client.GetFromJsonAsync<List<VideoMissingThumbnailModel>>(
            ApiRoutes.Converter.VideosMissingThumbnails + query, JsonOptions, Cancellation);

        Assert.NotNull(response);
        Assert.Equal(videos.Take(expectedCount).Select(v => v.Id), response.Select(v => v.VideoId));
        Assert.Equal(videos.Take(expectedCount).Select(v => v.SourceBlobId), response.Select(v => v.SourceBlobId));
        // Listing candidates does not claim conversion work.
        db.ChangeTracker.Clear();
        Assert.All(await db.Videos.Where(v => videos.Select(x => x.Id).Contains(v.Id)).ToListAsync(Cancellation),
            v => Assert.Null(v.LockedTill));
    }

    [Fact]
    public async Task GetVideosMissingThumbnails_ExcludesIneligibleVideos_AndAllowsExpiredLocks()
    {
        await MakeAllExistingVideosIneligibleForThumbnail();
        var owner = new UserDataBuilder().Build();
        var notConverted = new VideoDataBuilder().OwnedBy(owner).Build();
        var hasThumbnail = new VideoDataBuilder().OwnedBy(owner).Converted().WithThumbnailBlobId("thumb").Build();
        var locked = new VideoDataBuilder().OwnedBy(owner).Converted().Build();
        locked.LockedTill = DateTime.UtcNow.AddHours(1);
        var expired = new VideoDataBuilder().OwnedBy(owner).Converted().Build();
        expired.LockedTill = DateTime.UtcNow.AddHours(-1);
        await using var db = App.CreateDbContext();
        db.AddRange(owner, notConverted, hasThumbnail, locked, expired);
        await db.SaveChangesAsync(Cancellation);
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);

        var response = await client.GetFromJsonAsync<List<VideoMissingThumbnailModel>>(
            ApiRoutes.Converter.VideosMissingThumbnails, JsonOptions, Cancellation);

        Assert.NotNull(response);
        Assert.Equal(expired.Id, Assert.Single(response).VideoId);
    }

    [Fact]
    public async Task GetVideosMissingThumbnails_ReturnsEmptyArray_WhenNoCandidates()
    {
        await MakeAllExistingVideosIneligibleForThumbnail();
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);

        var response = await client.GetFromJsonAsync<List<VideoMissingThumbnailModel>>(
            ApiRoutes.Converter.VideosMissingThumbnails, JsonOptions, Cancellation);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("invalid")]
    public async Task GetVideosMissingThumbnails_RejectsInvalidLimit(string limit)
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);
        var response = await client.GetAsync(ApiRoutes.Converter.VideosMissingThumbnails + "?limit=" + limit, Cancellation);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetVideosMissingThumbnails_WithReadScopeOnly_ReturnsForbidden()
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);
        var response = await client.GetAsync(ApiRoutes.Converter.VideosMissingThumbnails, Cancellation);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetVideosMissingThumbnails_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await App.CreateAnonymousClient().GetAsync(ApiRoutes.Converter.VideosMissingThumbnails, Cancellation);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetThumbnails_WithReadScopeOnly_ReturnsForbidden()
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.GetAsync(ApiRoutes.Converter.Thumbnails, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetThumbnailSas_WithReadScopeOnly_ReturnsForbidden()
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.GetAsync(ThumbnailSasRoute(Guid.NewGuid()), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublishThumbnail_WithReadScopeOnly_ReturnsForbidden()
    {
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.PostAsync(PublishThumbnailRoute(Guid.NewGuid()), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ThumbnailPublishFlow_EndToEnd_SetsBlobId_PopulatesUrl_AndDropsFromMissingList()
    {
        await MakeAllExistingVideosIneligibleForThumbnail();

        // Arrange: a converted video, privately shared with its owner, with no thumbnail yet.
        var owner = new UserDataBuilder().Build();
        var blobId = Guid.NewGuid().ToString();
        var videoBuilder = new VideoDataBuilder()
            .OwnedBy(owner)
            .Converted()
            .WithBlobId(blobId)
            .ShareAsPrivate(owner);
        var video = videoBuilder.Build();
        var share = videoBuilder.BuildShares().Single();

        await using var db = App.CreateDbContext();
        db.AddRange(owner, video, share);
        await db.SaveChangesAsync(Cancellation);

        // Source blob must exist (or converted as fallback) for the converter listing to return it.
        var blobs = App.CreateBlobFactory().GetBlobDataService(BlobContainer.VideosToConvert);
        await blobs.Upload(video.SourceBlobId, new MemoryStream([1, 2, 3]));

        var converterClient = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Convert);
        var ownerClient = App.CreateAuthorizedClient(owner.Id, ApiScopes.Read);

        // 1. Converter sees it in the missing-thumbnail listing.
        var beforePublish = await converterClient.GetFromJsonAsync<VideoToThumbnailResponse>(
            ApiRoutes.Converter.Thumbnails, JsonOptions, Cancellation);
        Assert.NotNull(beforePublish);
        Assert.True(beforePublish!.VideoExists);
        Assert.Equal(video.Id, beforePublish.VideoToThumbnail!.Id);

        // 2. Converter requests an upload SAS, then "uploads" the thumbnail straight to Azurite
        //    (mirrors the pattern used by VideoUploaderTests for the converted-video blob).
        var sasResponse = await converterClient.GetFromJsonAsync<GetThumbnailSasResponse>(
            ThumbnailSasRoute(video.Id), JsonOptions, Cancellation);
        Assert.NotNull(sasResponse);
        Assert.False(string.IsNullOrWhiteSpace(sasResponse!.Sas));

        var thumbnails = App.CreateBlobFactory().GetBlobDataService(BlobContainer.Thumbnails);
        var expectedThumbnailBlobId = $"{video.Id}/thumbnail.jpg";
        await thumbnails.Upload(expectedThumbnailBlobId, new MemoryStream([0xFF, 0xD8, 0xFF]));

        // 3. Converter publishes the thumbnail.
        var publishResponse = await converterClient.PostAsync(PublishThumbnailRoute(video.Id), content: null, Cancellation);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        db.ChangeTracker.Clear();
        var updated = await db.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id, Cancellation);
        Assert.Equal(expectedThumbnailBlobId, updated.ThumbnailBlobId);

        // 4. The owner now sees a non-null ThumbnailUrl via GET /video/{blobId}.
        var infoResponse = await ownerClient.GetFromJsonAsync<VideoInformationResponse>(
            VideoInfoRoute(blobId), JsonOptions, Cancellation);
        Assert.NotNull(infoResponse);
        Assert.False(string.IsNullOrWhiteSpace(infoResponse!.VideoInformation.ThumbnailUrl));

        // 5. The video has dropped out of the missing-thumbnail listing.
        var afterPublish = await converterClient.GetFromJsonAsync<VideoToThumbnailResponse>(
            ApiRoutes.Converter.Thumbnails, JsonOptions, Cancellation);
        Assert.NotNull(afterPublish);
        if (afterPublish!.VideoExists)
            Assert.NotEqual(video.Id, afterPublish.VideoToThumbnail!.Id);
    }

    [Fact]
    public async Task GetVideoInfo_UserWithoutAccess_ReturnsNotFound()
    {
        // Arrange: a converted, thumbnailed video shared privately with its owner only.
        var owner = new UserDataBuilder().Build();
        var stranger = new UserDataBuilder().Build();
        var blobId = Guid.NewGuid().ToString();
        var videoBuilder = new VideoDataBuilder()
            .OwnedBy(owner)
            .Converted()
            .WithBlobId(blobId)
            .WithThumbnailBlobId($"{Guid.NewGuid()}/thumbnail.jpg")
            .ShareAsPrivate(owner);
        var video = videoBuilder.Build();
        var share = videoBuilder.BuildShares().Single();

        await using var db = App.CreateDbContext();
        db.AddRange(owner, stranger, video, share);
        await db.SaveChangesAsync(Cancellation);

        var strangerClient = App.CreateAuthorizedClient(stranger.Id, ApiScopes.Read);

        var response = await strangerClient.GetAsync(VideoInfoRoute(blobId), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
