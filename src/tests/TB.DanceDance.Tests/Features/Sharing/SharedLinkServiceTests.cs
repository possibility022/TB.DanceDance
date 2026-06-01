using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Tests.Features.Sharing;

/// <summary>
/// Public short-link (view-sharing) feature: <see cref="SharedLinkHandlers"/>. Create-time authorization
/// (owner OR has-access) reaches the Access module through the mediator's
/// <see cref="DoesUserHaveAccessToVideoQuery"/>; everything else is local to Videos.
/// </summary>
public class SharedLinkServiceTests : BaseTestClass
{
    public SharedLinkServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    private Task<SharedLink?> GetLink(string id) =>
        SeedVideosContext.SharedLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, TestContext.Current.CancellationToken);

    [Fact]
    public async Task CreateSharedLinkAsync_VideoOwner_CreatesLink()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = owner.Id, ExpirationDays = 7, AllowComments = true, AllowAnonymousComments = false },
            TestContext.Current.CancellationToken);

        Assert.NotNull(link);
        Assert.NotEmpty(link.Id);
        Assert.Equal(8, link.Id.Length); // Base62 8-char
        Assert.Equal(video.Id, link.VideoId);
        Assert.Equal(owner.Id, link.SharedBy);
        Assert.False(link.IsRevoked);
        Assert.True(link.ExpireAt > link.CreatedAt);
        Assert.Equal(7, (link.ExpireAt - link.CreatedAt).Days);
        Assert.True(link.AllowComments);
        Assert.False(link.AllowAnonymousComments);

        var saved = await GetLink(link.Id);
        Assert.NotNull(saved);
        Assert.True(saved!.AllowComments);
        Assert.False(saved.AllowAnonymousComments);
    }

    [Fact]
    public async Task CreateSharedLinkAsync_UserWithSharedWithAccess_CreatesLink()
    {
        var owner = new UserDataBuilder().Build();
        var otherUserB = new UserDataBuilder();
        var otherUser = otherUserB.Build();
        var group = new GroupDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareWithGroup(group, owner).Build();
        var groupAssignment = otherUserB.AssignTo(group, DateTime.UtcNow.AddDays(-10));

        SeedAccessContext.AddRange(owner, otherUser, group, groupAssignment);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = otherUser.Id, ExpirationDays = 30, AllowComments = true, AllowAnonymousComments = false },
            TestContext.Current.CancellationToken);

        Assert.NotNull(link);
        Assert.Equal(video.Id, link.VideoId);
        Assert.Equal(otherUser.Id, link.SharedBy);
        Assert.Equal(30, (link.ExpireAt - link.CreatedAt).Days);
    }

    [Fact]
    public async Task CreateSharedLinkAsync_UnauthorizedUser_ThrowsArgumentException()
    {
        var owner = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedAccessContext.AddRange(owner, unauthorized);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = unauthorized.Id, ExpirationDays = 7, AllowComments = true, AllowAnonymousComments = false },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSharedLinkAsync_InvalidVideoId_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateSharedLinkCommand { VideoId = Guid.NewGuid(), UserId = user.Id, ExpirationDays = 7, AllowComments = true, AllowAnonymousComments = false },
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task CreateSharedLinkAsync_ValidExpirationDays_CreatesLink(int days)
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = owner.Id, ExpirationDays = days, AllowComments = true, AllowAnonymousComments = false },
            TestContext.Current.CancellationToken);

        Assert.NotNull(link);
        Assert.Equal(days, (link.ExpireAt - link.CreatedAt).Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(1000)]
    public async Task CreateSharedLinkAsync_InvalidExpirationDays_ThrowsArgumentException(int days)
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = owner.Id, ExpirationDays = days, AllowComments = true, AllowAnonymousComments = false },
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_ActiveLink_ReturnsVideo()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).WithName("Test Video").Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).ExpiresInDays(7).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetVideoBySharedLinkQuery(link.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(video.Id, result!.Id);
        Assert.Equal("Test Video", result.Name);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_ExpiredLink_ReturnsNull()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1)).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetVideoBySharedLinkQuery(link.Id), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_RevokedLink_ReturnsNull()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).ExpiresInDays(7).Revoked(true).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetVideoBySharedLinkQuery(link.Id), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_NonExistentLink_ReturnsNull()
    {
        var result = await Send(new GetVideoBySharedLinkQuery("notexist"), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_LinkCreator_RevokesSuccessfully()
    {
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(creator).ExpiresInDays(7).Build();
        SeedAccessContext.AddRange(creator, owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new RevokeSharedLinkCommand(link.Id, creator.Id), TestContext.Current.CancellationToken);

        Assert.True(result);
        var revoked = await GetLink(link.Id);
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_VideoOwner_RevokesSuccessfully()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(otherUser).ExpiresInDays(7).Build();
        SeedAccessContext.AddRange(owner, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new RevokeSharedLinkCommand(link.Id, owner.Id), TestContext.Current.CancellationToken);

        Assert.True(result);
        var revoked = await GetLink(link.Id);
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_UnauthorizedUser_ReturnsFalse()
    {
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(creator).ExpiresInDays(7).Build();
        SeedAccessContext.AddRange(creator, owner, unauthorized);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new RevokeSharedLinkCommand(link.Id, unauthorized.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
        var notRevoked = await GetLink(link.Id);
        Assert.False(notRevoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_NonExistentLink_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new RevokeSharedLinkCommand("notexist", user.Id), TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_AlreadyRevoked_Idempotent_ReturnsTrue()
    {
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(creator).ExpiresInDays(7).Revoked(true).Build();
        SeedAccessContext.AddRange(creator, owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new RevokeSharedLinkCommand(link.Id, creator.Id), TestContext.Current.CancellationToken);
        Assert.True(result);
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_ReturnsLinksCreatedByUser()
    {
        var user = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video1 = new VideoDataBuilder().UploadedBy(owner).WithName("Video 1").Build();
        var video2 = new VideoDataBuilder().UploadedBy(owner).WithName("Video 2").Build();
        var link1 = new SharedLinkDataBuilder().WithId("link0001").ForVideo(video1).SharedBy(user).Build();
        var link2 = new SharedLinkDataBuilder().WithId("link0002").ForVideo(video2).SharedBy(user).Build();

        SeedAccessContext.AddRange(user, owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video1, video2, link1, link2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserSharedLinksQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Id == "link0001");
        Assert.Contains(result, l => l.Id == "link0002");
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_ReturnsLinksForVideosOwnedByUser()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video1 = new VideoDataBuilder().UploadedBy(owner).WithName("Owned Video 1").Build();
        var video2 = new VideoDataBuilder().UploadedBy(owner).WithName("Owned Video 2").Build();
        var link1 = new SharedLinkDataBuilder().WithId("link0003").ForVideo(video1).SharedBy(otherUser).Build();
        var link2 = new SharedLinkDataBuilder().WithId("link0004").ForVideo(video2).SharedBy(otherUser).Build();

        SeedAccessContext.AddRange(owner, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video1, video2, link1, link2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserSharedLinksQuery(owner.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Id == "link0003");
        Assert.Contains(result, l => l.Id == "link0004");
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_EmptyList_ForUserWithNoLinks()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserSharedLinksQuery(user.Id), TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_OrderedByCreatedAtDescending()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).Build();
        var link1 = new SharedLinkDataBuilder().WithId("link0005").ForVideo(video).SharedBy(user).CreatedAt(DateTimeOffset.UtcNow.AddDays(-2)).Build();
        var link2 = new SharedLinkDataBuilder().WithId("link0006").ForVideo(video).SharedBy(user).CreatedAt(DateTimeOffset.UtcNow.AddDays(-1)).Build();
        var link3 = new SharedLinkDataBuilder().WithId("link0007").ForVideo(video).SharedBy(user).CreatedAt(DateTimeOffset.UtcNow).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link1, link2, link3);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserSharedLinksQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        var list = result.ToList();
        Assert.Equal("link0007", list[0].Id);
        Assert.Equal("link0006", list[1].Id);
        Assert.Equal("link0005", list[2].Id);
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_IncludesVideoDetails()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithName("Detailed Video").WithDuration(TimeSpan.FromMinutes(5)).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserSharedLinksQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Single(result);
        var returnedLink = result.First();
        Assert.NotNull(returnedLink.Video);
        Assert.Equal("Detailed Video", returnedLink.Video!.Name);
        Assert.Equal(TimeSpan.FromMinutes(5), returnedLink.Video.Duration);
    }

    [Fact]
    public async Task CreateSharedLink_WithCommentSettings_StoresCorrectly()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await Send(new CreateSharedLinkCommand { VideoId = video.Id, UserId = owner.Id, ExpirationDays = 7, AllowComments = false, AllowAnonymousComments = true },
            TestContext.Current.CancellationToken);

        Assert.False(link.AllowComments);
        Assert.True(link.AllowAnonymousComments);

        var saved = await GetLink(link.Id);
        Assert.NotNull(saved);
        Assert.False(saved!.AllowComments);
        Assert.True(saved.AllowAnonymousComments);
    }

    [Fact]
    public async Task GetSharedLinkAsync_IncludesCommentSettings()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).WithName("Video with Comments").WithCommentVisibility(CommentVisibility.AuthenticatedOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).ExpiresInDays(7).AllowComments(true).AllowAnonymousComments(false).Build();
        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetSharedLinkQuery(link.Id), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(link.Id, result!.Id);
        Assert.True(result.AllowComments);
        Assert.False(result.AllowAnonymousComments);
        Assert.NotNull(result.Video);
        Assert.Equal(video.Id, result.Video!.Id);
        Assert.Equal((int)CommentVisibility.AuthenticatedOnly, result.Video.CommentVisibility);
    }
}
