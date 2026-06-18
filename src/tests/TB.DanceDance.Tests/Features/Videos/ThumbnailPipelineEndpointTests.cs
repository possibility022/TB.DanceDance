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
            .UploadedBy(owner)
            .Converted()
            .WithBlobId(blobId)
            .ShareAsPrivate(owner);
        var video = videoBuilder.Build();
        var share = videoBuilder.BuildShares().Single();

        await using var db = App.CreateDbContext();
        db.AddRange(owner, video, share);
        await db.SaveChangesAsync(Cancellation);

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
            .UploadedBy(owner)
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
