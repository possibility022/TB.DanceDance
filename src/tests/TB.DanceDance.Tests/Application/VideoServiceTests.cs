using Application.Services;
using Domain;
using Domain.Entities;
using Domain.Models;
using Domain.Services;
using Infrastructure.Data;
using Infrastructure.Data.BlobStorage;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Application;

public class VideoServiceTests : BaseTestClass
{
    private readonly BlobStorageFixture blobStorageFixture;

    private BlobDataServiceFactory factory = null!;
    private readonly IVideoUploaderService uploaderService;
    private readonly IAccessService accessService;

    private VideoService videoService = null!;

    public VideoServiceTests(DanceDbFixture danceDbFixture, BlobStorageFixture blobStorageFixture) : base(
        danceDbFixture)
    {
        uploaderService = Substitute.For<IVideoUploaderService>();
        accessService = Substitute.For<IAccessService>();
        this.blobStorageFixture = blobStorageFixture;
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
        videoService = new VideoService(runtimeDbContext, factory, uploaderService, accessService);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetVideoByBlobAsync_ReturnsNull_WhenAccessDenied()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedDbContext.AddRange(user, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken)
            .Returns(false);

        var result = await videoService.GetVideoByBlobAsync(user.Id, video.BlobId!, TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoByBlobAsync_ReturnsVideo_WhenAccessGranted()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedDbContext.AddRange(user, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken)
            .Returns(true);

        var result = await videoService.GetVideoByBlobAsync(user.Id, video.BlobId!, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(video.Id, result!.Id);
    }

    [Fact]
    public async Task RenameVideoAsync_UpdatesName_AndReturnsTrue()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithName("Old Name").Build();
        SeedDbContext.AddRange(user, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await videoService.RenameVideoAsync(video.Id, "New Name", TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(ok);
        var updated = await SeedDbContext.Videos.FindAsync([video.Id], TestContext.Current.CancellationToken);
        Assert.Equal("New Name", updated!.Name);
    }

    [Fact]
    public async Task RenameVideoAsync_ReturnsFalse_WhenNotFound()
    {
        var ok = await videoService.RenameVideoAsync(Guid.NewGuid(), "Name", TestContext.Current.CancellationToken);
        Assert.False(ok);
    }

    [Fact]
    public async Task GetSharingLink_ForExistingVideo_ReturnsContext_FromUploaderService()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithSourceBlobId("src-123").Build();
        SeedDbContext.AddRange(user, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var sas = new SharedBlob { BlobId = video.SourceBlobId, Sas = new Uri("https://example/sas"), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        uploaderService.GetUploadSasUri(video.SourceBlobId).Returns(sas);

        var ctx = await videoService.GetSharingLink(video.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(ctx);
        Assert.Equal(video.Id, ctx!.VideoId);
        Assert.Equal(video.SourceBlobId, ctx.SourceBlobId);
        Assert.Equal(sas.Sas, ctx.Sas);
    }

    [Fact]
    public async Task GetSharingLink_ForExistingVideo_ReturnsNull_WhenVideoMissing()
    {
        var ctx = await videoService.GetSharingLink(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(ctx);
    }

    [Fact]
    public async Task GetSharingLink_CreatesVideo_AndShare_ForEvent()
    {
        var user = new UserDataBuilder().Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        SeedDbContext.AddRange(owner, evt, user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var shared = new SharedBlob { BlobId = Guid.NewGuid().ToString(), Sas = new Uri("https://upload/sas1"), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        uploaderService.GetUploadSasUri().Returns(shared);

        var ctx = await videoService.GetSharingLink(user.Id, "VidName", "file.mp4", assignedToEvent: true, sharedWith: evt.Id, TestContext.Current.CancellationToken);

        // Verify persisted
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.Videos.AsQueryable().Where(v => v.Id == ctx.VideoId).FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(shared.BlobId, saved.SourceBlobId);
        Assert.Equal(user.Id, saved.UploadedBy);
        Assert.False(saved.Converted);
        // SharedWith
        var link = SeedDbContext.SharedWith.Single(sw => sw.VideoId == saved.Id);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(evt.Id, link.EventId);
        Assert.Null(link.GroupId);

        uploaderService.Received(1).GetUploadSasUri();
    }

    [Fact]
    public async Task GetSharingLink_CreatesVideo_AndShare_ForGroup()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(user, group);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var shared = new SharedBlob { BlobId = Guid.NewGuid().ToString(), Sas = new Uri("https://upload/sas2"), ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) };
        uploaderService.GetUploadSasUri().Returns(shared);

        var ctx = await videoService.GetSharingLink(user.Id, "VidName", "file.mp4", assignedToEvent: false, sharedWith: group.Id, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.Videos.AsQueryable().Where(v => v.Id == ctx.VideoId).FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(shared.BlobId, saved.SourceBlobId);
        var link = SeedDbContext.SharedWith.Single(sw => sw.VideoId == saved.Id);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(group.Id, link.GroupId);
        Assert.Null(link.EventId);

        uploaderService.Received(1).GetUploadSasUri();
    }

    [Fact]
    public async Task OpenStream_ReturnsStream_ForExistingBlob()
    {
        // Arrange: upload a small blob to the container used by VideoService
        var blobSvc = factory.GetBlobDataService(BlobContainer.Videos);
        var blobId = Guid.NewGuid().ToString();
        await blobSvc.Upload(blobId, new MemoryStream([1,2,3,4]));

        // Act
        await using var stream = await videoService.OpenStream(blobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(stream);
        var buffer = new byte[4];
        var read = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);
        Assert.Equal(4, read);
        Assert.Equal(new byte[] {1,2,3,4}, buffer);
    }
}