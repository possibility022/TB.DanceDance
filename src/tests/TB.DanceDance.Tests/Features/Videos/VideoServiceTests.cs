using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Tests.Features.Videos;

/// <summary>
/// Video viewing/management/upload handlers (Videos module). The previously-mocked access service and
/// uploader service are now the real wiring: access is decided by seeding shares, and SAS issuance hits
/// Azurite through the blob factory.
/// </summary>
public class VideoServiceTests : BaseTestClass
{
    private readonly BlobStorageFixture blobStorageFixture;
    private IBlobDataServiceFactory factory = null!;

    public VideoServiceTests(DanceDbFixture danceDbFixture, BlobStorageFixture blobStorageFixture) : base(danceDbFixture)
    {
        this.blobStorageFixture = blobStorageFixture;
    }

    protected override string BlobConnectionString => blobStorageFixture.GetConnectionString();

    protected override ValueTask Initialize()
    {
        factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task GetVideoByBlobAsync_ReturnsNull_WhenAccessDenied()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetVideoForViewingQuery(user.Id, video.BlobId!), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoByBlobAsync_ReturnsVideo_WhenAccessGranted()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).ShareAsPrivate(user).Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetVideoForViewingQuery(user.Id, video.BlobId!), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(video.Id, result!.Id);
    }

    [Fact]
    public async Task RenameVideoAsync_UpdatesName_AndReturnsTrue()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithName("Old Name").Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new RenameVideoCommand { VideoId = video.Id, NewName = "New Name" }, TestContext.Current.CancellationToken);
        Assert.True(ok);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task RenameVideoAsync_ReturnsFalse_WhenNotFound()
    {
        var ok = await Send(new RenameVideoCommand { VideoId = Guid.NewGuid(), NewName = "Name" }, TestContext.Current.CancellationToken);
        Assert.False(ok);
    }

    [Fact]
    public async Task GetSharingLink_ForExistingVideo_ReturnsContext_FromUploaderService()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithSourceBlobId("src-123").Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ctx = await Send(new CreateSharingLinkCommand { VideoId = video.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(ctx);
        Assert.Equal(video.Id, ctx!.VideoId);
        Assert.Equal(video.SourceBlobId, ctx.SourceBlobId);
        Assert.True(ctx.Sas.IsAbsoluteUri);
    }

    [Fact]
    public async Task GetSharingLink_ForExistingVideo_ReturnsNull_WhenVideoMissing()
    {
        var ctx = await Send(new CreateSharingLinkCommand { VideoId = Guid.NewGuid() }, TestContext.Current.CancellationToken);
        Assert.Null(ctx);
    }

    [Fact]
    public async Task GetSharingLink_CreatesVideo_AndShare_ForEvent()
    {
        var user = new UserDataBuilder().Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        SeedAccessContext.AddRange(owner, evt, user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ctx = await Send(new CreateVideoUploadCommand
        {
            UserId = user.Id, Name = "VidName", FileName = "file.mp4", SharingWithType = SharingWithType.Event, SharedWith = evt.Id
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(ctx);
        var saved = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == ctx!.VideoId, TestContext.Current.CancellationToken);
        Assert.Equal(ctx!.SourceBlobId, saved.SourceBlobId);
        Assert.Equal(user.Id, saved.UploadedBy);
        Assert.False(saved.Converted);
        var link = await SeedVideosContext.SharedWith.AsNoTracking().SingleAsync(sw => sw.VideoId == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(evt.Id, link.EventId);
        Assert.Null(link.GroupId);
    }

    [Fact]
    public async Task GetSharingLink_CreatesVideo_AndShare_ForGroup()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedAccessContext.AddRange(user, group);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ctx = await Send(new CreateVideoUploadCommand
        {
            UserId = user.Id, Name = "VidName", FileName = "file.mp4", SharingWithType = SharingWithType.Group, SharedWith = group.Id
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(ctx);
        var saved = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == ctx!.VideoId, TestContext.Current.CancellationToken);
        var link = await SeedVideosContext.SharedWith.AsNoTracking().SingleAsync(sw => sw.VideoId == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(group.Id, link.GroupId);
        Assert.Null(link.EventId);
    }

    [Fact]
    public async Task GetSharingLink_CreatesVideo_AndShare_ForPrivateVideo()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ctx = await Send(new CreateVideoUploadCommand
        {
            UserId = user.Id, Name = "MyPrivateVideo", FileName = "private.mp4", SharingWithType = SharingWithType.Private, SharedWith = null
        }, TestContext.Current.CancellationToken);

        Assert.NotNull(ctx);
        Assert.True(ctx!.Sas.IsAbsoluteUri);

        var saved = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == ctx.VideoId, TestContext.Current.CancellationToken);
        Assert.Equal(ctx.SourceBlobId, saved.SourceBlobId);
        Assert.Equal(user.Id, saved.UploadedBy);
        Assert.Equal("MyPrivateVideo", saved.Name);
        Assert.False(saved.Converted);

        var link = await SeedVideosContext.SharedWith.AsNoTracking().SingleAsync(sw => sw.VideoId == saved.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, link.UserId);
        Assert.Null(link.EventId);
        Assert.Null(link.GroupId);
    }

    [Fact]
    public async Task GetSharingLink_ThrowsArgumentException_ForInvalidSharingType()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateVideoUploadCommand
            {
                UserId = user.Id, Name = "Video", FileName = "file.mp4", SharingWithType = SharingWithType.NotSpecified, SharedWith = null
            }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenStream_ReturnsStream_ForExistingBlob()
    {
        var blobSvc = factory.GetBlobDataService(BlobContainer.Videos);
        var blobId = Guid.NewGuid().ToString();
        await blobSvc.Upload(blobId, new MemoryStream([1, 2, 3, 4]));

        await using var stream = await Send(new OpenVideoStreamQuery(blobId), TestContext.Current.CancellationToken);

        Assert.NotNull(stream);
        var buffer = new byte[4];
        var read = await stream.ReadAsync(buffer, TestContext.Current.CancellationToken);
        Assert.Equal(4, read);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, buffer);
    }

    [Fact]
    public async Task UpdateCommentVisibility_VideoOwner_UpdatesSuccessfully()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentVisibilityCommand
        {
            VideoId = video.Id, UserId = owner.Id, CommentVisibility = (int)CommentVisibility.OwnerOnly
        }, TestContext.Current.CancellationToken);

        Assert.True(result);
        var updated = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(CommentVisibility.OwnerOnly, updated.CommentVisibility);
    }

    [Fact]
    public async Task UpdateCommentVisibility_NotVideoOwner_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().WithId("other-user-id").Build();
        var video = new VideoDataBuilder().UploadedBy(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        SeedAccessContext.AddRange(owner, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentVisibilityCommand
        {
            VideoId = video.Id, UserId = otherUser.Id, CommentVisibility = (int)CommentVisibility.OwnerOnly
        }, TestContext.Current.CancellationToken);

        Assert.False(result);
        var unchanged = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(CommentVisibility.Public, unchanged.CommentVisibility);
    }

    [Fact]
    public async Task UpdateCommentVisibility_NonExistentVideo_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentVisibilityCommand
        {
            VideoId = Guid.NewGuid(), UserId = user.Id, CommentVisibility = (int)CommentVisibility.OwnerOnly
        }, TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
