using Application.Services;
using Domain.Entities;
using Domain.Services;
using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Application;

public class SharedLinkServiceTests : BaseTestClass
{
    private ISharedLinkService sharedLinkService = null!;
    private IAccessService accessService = null!;

    public SharedLinkServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        accessService = new AccessService(runtimeDbContext);
        sharedLinkService = new SharedLinkService(runtimeDbContext, accessService);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task CreateSharedLinkAsync_VideoOwner_CreatesLink()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedDbContext.AddRange(owner, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var link = await sharedLinkService.CreateSharedLinkAsync(
            video.Id, owner.Id, 7, true, false, TestContext.Current.CancellationToken);

        // Assert
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

        // Verify persisted
        var saved = await SeedDbContext.Set<SharedLink>().FindAsync([link.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(link.Id, saved!.Id);
        Assert.True(saved.AllowComments);
        Assert.False(saved.AllowAnonymousComments);
    }

    [Fact]
    public async Task CreateSharedLinkAsync_UserWithSharedWithAccess_CreatesLink()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareWithGroup(group, owner).Build();
        var groupAssignment = new AssignedToGroup
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = otherUser.Id,
            WhenJoined = DateTime.UtcNow.AddDays(-10)
        };

        SeedDbContext.AddRange(owner, otherUser, group, video, groupAssignment);
        var sharedWith = new SharedWith
        {
            Id = Guid.NewGuid(),
            VideoId = video.Id,
            UserId = owner.Id,
            GroupId = group.Id
        };
        SeedDbContext.Add(sharedWith);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var link = await sharedLinkService.CreateSharedLinkAsync(
            video.Id, otherUser.Id, 30, true, false, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(link);
        Assert.Equal(video.Id, link.VideoId);
        Assert.Equal(otherUser.Id, link.SharedBy);
        Assert.Equal(30, (link.ExpireAt - link.CreatedAt).Days);
    }

    [Fact]
    public async Task CreateSharedLinkAsync_UnauthorizedUser_ThrowsArgumentException()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedDbContext.AddRange(owner, unauthorized, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await sharedLinkService.CreateSharedLinkAsync(
                video.Id, unauthorized.Id, 7, true, false, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateSharedLinkAsync_InvalidVideoId_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await sharedLinkService.CreateSharedLinkAsync(
                Guid.NewGuid(), user.Id, 7, true, false, TestContext.Current.CancellationToken);
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public async Task CreateSharedLinkAsync_ValidExpirationDays_CreatesLink(int days)
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedDbContext.AddRange(owner, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var link = await sharedLinkService.CreateSharedLinkAsync(
            video.Id, owner.Id, days, true, false, TestContext.Current.CancellationToken);

        // Assert
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
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedDbContext.AddRange(owner, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await sharedLinkService.CreateSharedLinkAsync(
                video.Id, owner.Id, days, true, false, TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_ActiveLink_ReturnsVideo()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).WithName("Test Video").Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .ExpiresInDays(7)
            .Build();
        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetVideoBySharedLinkAsync(
            link.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(video.Id, result!.Id);
        Assert.Equal("Test Video", result.Name);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_ExpiredLink_ReturnsNull()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1)) // Expired yesterday
            .Build();
        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetVideoBySharedLinkAsync(
            link.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_RevokedLink_ReturnsNull()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .ExpiresInDays(7)
            .Revoked(true)
            .Build();
        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetVideoBySharedLinkAsync(
            link.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVideoBySharedLinkAsync_NonExistentLink_ReturnsNull()
    {
        // Act
        var result = await sharedLinkService.GetVideoBySharedLinkAsync(
            "notexist", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_LinkCreator_RevokesSuccessfully()
    {
        // Arrange
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(creator)
            .ExpiresInDays(7)
            .Build();
        SeedDbContext.AddRange(creator, owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.RevokeSharedLinkAsync(
            link.Id, creator.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        // Verify revoked in DB
        SeedDbContext.ChangeTracker.Clear();
        var revoked = await SeedDbContext.Set<SharedLink>().FindAsync([link.Id], TestContext.Current.CancellationToken);
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_VideoOwner_RevokesSuccessfully()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(otherUser) // Created by someone else
            .ExpiresInDays(7)
            .Build();
        SeedDbContext.AddRange(owner, otherUser, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Owner revokes link created by otherUser
        var result = await sharedLinkService.RevokeSharedLinkAsync(
            link.Id, owner.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        // Verify revoked in DB
        SeedDbContext.ChangeTracker.Clear();
        var revoked = await SeedDbContext.Set<SharedLink>().FindAsync([link.Id], TestContext.Current.CancellationToken);
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_UnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(creator)
            .ExpiresInDays(7)
            .Build();
        SeedDbContext.AddRange(creator, owner, unauthorized, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.RevokeSharedLinkAsync(
            link.Id, unauthorized.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        // Verify NOT revoked in DB
        SeedDbContext.ChangeTracker.Clear();
        var notRevoked = await SeedDbContext.Set<SharedLink>().FindAsync([link.Id], TestContext.Current.CancellationToken);
        Assert.False(notRevoked!.IsRevoked);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_NonExistentLink_ReturnsFalse()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.RevokeSharedLinkAsync(
            "notexist", user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeSharedLinkAsync_AlreadyRevoked_Idempotent_ReturnsTrue()
    {
        // Arrange
        var creator = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(creator)
            .ExpiresInDays(7)
            .Revoked(true) // Already revoked
            .Build();
        SeedDbContext.AddRange(creator, owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.RevokeSharedLinkAsync(
            link.Id, creator.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_ReturnsLinksCreatedByUser()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var video1 = new VideoDataBuilder().UploadedBy(owner).WithName("Video 1").Build();
        var video2 = new VideoDataBuilder().UploadedBy(owner).WithName("Video 2").Build();
        var link1 = new SharedLinkDataBuilder().WithId("link0001").ForVideo(video1).SharedBy(user).Build();
        var link2 = new SharedLinkDataBuilder().WithId("link0002").ForVideo(video2).SharedBy(user).Build();

        SeedDbContext.AddRange(user, owner, video1, video2, link1, link2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetUserSharedLinksAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Id == "link0001");
        Assert.Contains(result, l => l.Id == "link0002");
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_ReturnsLinksForVideosOwnedByUser()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video1 = new VideoDataBuilder().UploadedBy(owner).WithName("Owned Video 1").Build();
        var video2 = new VideoDataBuilder().UploadedBy(owner).WithName("Owned Video 2").Build();
        // Links created by other users but for owner's videos
        var link1 = new SharedLinkDataBuilder().WithId("link0003").ForVideo(video1).SharedBy(otherUser).Build();
        var link2 = new SharedLinkDataBuilder().WithId("link0004").ForVideo(video2).SharedBy(otherUser).Build();

        SeedDbContext.AddRange(owner, otherUser, video1, video2, link1, link2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetUserSharedLinksAsync(
            owner.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Id == "link0003");
        Assert.Contains(result, l => l.Id == "link0004");
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_EmptyList_ForUserWithNoLinks()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetUserSharedLinksAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_OrderedByCreatedAtDescending()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).Build();
        var link1 = new SharedLinkDataBuilder()
            .WithId("link0005")
            .ForVideo(video)
            .SharedBy(user)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-2))
            .Build();
        var link2 = new SharedLinkDataBuilder()
            .WithId("link0006")
            .ForVideo(video)
            .SharedBy(user)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-1))
            .Build();
        var link3 = new SharedLinkDataBuilder()
            .WithId("link0007")
            .ForVideo(video)
            .SharedBy(user)
            .CreatedAt(DateTimeOffset.UtcNow)
            .Build();

        SeedDbContext.AddRange(user, video, link1, link2, link3);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetUserSharedLinksAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        var list = result.ToList();
        Assert.Equal("link0007", list[0].Id); // Most recent
        Assert.Equal("link0006", list[1].Id);
        Assert.Equal("link0005", list[2].Id); // Oldest
    }

    [Fact]
    public async Task GetUserSharedLinksAsync_IncludesVideoDetails()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(user)
            .WithName("Detailed Video")
            .WithDuration(TimeSpan.FromMinutes(5))
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetUserSharedLinksAsync(
            user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var returnedLink = result.First();
        Assert.NotNull(returnedLink.Video);
        Assert.Equal("Detailed Video", returnedLink.Video.Name);
        Assert.Equal(TimeSpan.FromMinutes(5), returnedLink.Video.Duration);
    }

    #region Comment Settings Tests

    [Fact]
    public async Task CreateSharedLink_WithCommentSettings_StoresCorrectly()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        SeedDbContext.AddRange(owner, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var link = await sharedLinkService.CreateSharedLinkAsync(
            video.Id, owner.Id, 7, allowComments: false, allowAnonymousComments: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(link);
        Assert.False(link.AllowComments);
        Assert.True(link.AllowAnonymousComments);

        // Verify persisted with comment settings
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.Set<SharedLink>().FindAsync([link.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.False(saved!.AllowComments);
        Assert.True(saved.AllowAnonymousComments);
    }

    [Fact]
    public async Task GetSharedLinkAsync_IncludesCommentSettings()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .WithName("Video with Comments")
            .WithCommentVisibility(CommentVisibility.AuthenticatedOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .ExpiresInDays(7)
            .AllowComments(true)
            .AllowAnonymousComments(false)
            .Build();
        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await sharedLinkService.GetSharedLinkAsync(link.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(link.Id, result!.Id);
        Assert.True(result.AllowComments);
        Assert.False(result.AllowAnonymousComments);
        Assert.NotNull(result.Video);
        Assert.Equal(video.Id, result.Video.Id);
        Assert.Equal(CommentVisibility.AuthenticatedOnly, result.Video.CommentVisibility);
    }

    #endregion
}
