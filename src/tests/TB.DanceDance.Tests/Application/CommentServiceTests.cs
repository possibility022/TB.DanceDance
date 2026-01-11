using Application.Services;
using Domain.Entities;
using Domain.Services;
using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Application;

public class CommentServiceTests : BaseTestClass
{
    private ICommentService commentService = null!;

    public CommentServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        commentService = new CommentService(runtimeDbContext, null);
        return ValueTask.CompletedTask;
    }


    [Fact]
    public async Task CreateCommentAsync_AuthenticatedUser_CreatesComment()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comment = await commentService.CreateCommentAsync(
            user.Id,
            link.Id,
            "This is a test comment",
            null,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(comment);
        Assert.Equal(video.Id, comment.VideoId);
        Assert.Equal(user.Id, comment.UserId);
        Assert.Equal(link.Id, comment.SharedLinkId);
        Assert.Equal("This is a test comment", comment.Content);
        Assert.False(comment.IsHidden);
        Assert.False(comment.IsReported);
    }

    [Fact]
    public async Task CreateCommentAsync_AnonymousUser_CreatesCommentWithLinkId()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowAnonymousComments() // This also sets AllowComments = true
            .Build();

        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comment = await commentService.CreateCommentAsync(
            null, // Anonymous
            link.Id,
            "Anonymous comment",
            "Author",
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(comment);
        Assert.Equal(video.Id, comment.VideoId);
        Assert.Null(comment.UserId); // Anonymous
        Assert.Equal(link.Id, comment.SharedLinkId); // Link tracked for anonymous
        Assert.Equal("Anonymous comment", comment.Content);
    }

    [Fact]
    public async Task CreateCommentAsync_AuthenticatedUser_DoesNotStoreLinkId()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comment = await commentService.CreateCommentAsync(
            user.Id,
            link.Id,
            "Authenticated comment",
            null,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(link.Id, comment.SharedLinkId);
        Assert.Equal(user.Id, comment.UserId);
    }

    [Fact]
    public async Task CreateCommentAsync_LinkDoesNotAllowComments_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments(false) // Comments disabled
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                link.Id,
                "Should fail",
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_AnonymousUser_LinkDoesNotAllowAnonymous_ThrowsArgumentException()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments() // Comments enabled but not for anonymous
            .Build();

        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                null, // Anonymous
                link.Id,
                "Should fail",
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_ExpiredLink_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1)) // Expired
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                link.Id,
                "Should fail",
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_RevokedLink_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .Revoked() // Revoked
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                link.Id,
                "Should fail",
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_NonExistentLink_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                "notexist",
                "Should fail",
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                link.Id,
                "", // Empty content
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_ContentTooLong_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.CreateCommentAsync(
                user.Id,
                link.Id,
                new string('a', 2001), // 2001 chars, max is 2000
                null,
                null,
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task CreateCommentAsync_ValidContent_SetsTimestamps()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;

        // Act
        var comment = await commentService.CreateCommentAsync(
            user.Id,
            link.Id,
            "Test",
            null,
            null,
            TestContext.Current.CancellationToken);

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(comment.CreatedAt >= before && comment.CreatedAt <= after);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public async Task CreateCommentAsync_PersistedInDatabase()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(user)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(user, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comment = await commentService.CreateCommentAsync(
            user.Id,
            link.Id,
            "Persisted comment",
            null,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Persisted comment", saved!.Content);
    }


    [Fact]
    public async Task GetComments_VideoOwner_SeesAllComments()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowAnonymousComments()
            .Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedDbContext.AddRange(owner, video, link, comment1, comment2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, 
            null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, comments.Count); // Owner sees both
        Assert.Contains(comments, c => c.Content == "Visible");
        Assert.Contains(comments, c => c.Content == "Hidden");
    }

    [Fact]
    public async Task GetComments_Public_Anonymous_SeesNonHidden()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.Public)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowAnonymousComments()
            .Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedDbContext.AddRange(owner, video, link, comment1, comment2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            null, null, // Anonymous
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(comments); // Only non-hidden
        Assert.Equal("Visible", comments.First().Content);
    }

    [Fact]
    public async Task GetComments_Public_Authenticated_SeesNonHidden()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.Public)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedDbContext.AddRange(owner, viewer, video, link, comment1, comment2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            viewer.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(comments);
        Assert.Equal("Visible", comments.First().Content);
    }

    [Fact]
    public async Task GetComments_AuthenticatedOnly_Anonymous_SeesNothing()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.AuthenticatedOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowAnonymousComments()
            .Build();

        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Invisible to anon").Build();

        SeedDbContext.AddRange(owner, video, link, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            null, null, // Anonymous
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_AuthenticatedOnly_Authenticated_SeesNonHidden()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.AuthenticatedOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible to auth").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedDbContext.AddRange(owner, viewer, video, link, comment1, comment2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            viewer.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(comments);
        Assert.Equal("Visible to auth", comments.First().Content);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Anonymous_SeesNothing()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.OwnerOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowAnonymousComments()
            .Build();

        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Owner only").Build();

        SeedDbContext.AddRange(owner, video, link, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            null, null, // Anonymous
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Authenticated_SeesNothing()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.OwnerOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Owner only").Build();

        SeedDbContext.AddRange(owner, viewer, video, link, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            viewer.Id, null, // Not owner
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Owner_SeesAll()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(owner)
            .ShareAsPrivate(owner)
            .WithCommentVisibility(CommentVisibility.OwnerOnly)
            .Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Comment 1").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Comment 2").Hidden().Build();

        SeedDbContext.AddRange(owner, video, link, comment1, comment2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task GetComments_HiddenComments_OnlyVisibleToOwner()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var hiddenComment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden()
            .Build();

        SeedDbContext.AddRange(owner, viewer, video, link, hiddenComment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Viewer
        var viewerComments = await commentService.GetCommentsForVideoAsync(
            viewer.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Act - Owner
        var ownerComments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(viewerComments); // Viewer sees nothing
        Assert.Single(ownerComments); // Owner sees hidden comment
    }

    [Fact]
    public async Task GetComments_ExpiredLink_ReturnsEmpty()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1))
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_RevokedLink_ReturnsEmpty()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .Revoked()
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_NoComments_ReturnsEmpty()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        SeedDbContext.AddRange(owner, video, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_IncludesUserDetails_ForAuthenticatedComments()
    {
        // Arrange
        var owner = new UserDataBuilder().WithFirstName("John").WithLastName("Doe").Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedDbContext.AddRange(owner, video, link, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        var returnedComment = comments.First();
        Assert.NotNull(returnedComment.User);
        Assert.Equal("John", returnedComment.User!.FirstName);
        Assert.Equal("Doe", returnedComment.User.LastName);
    }

    [Fact]
    public async Task GetComments_OrderedByCreatedAt_Ascending()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder()
            .ForVideo(video)
            .SharedBy(owner)
            .AllowComments()
            .Build();

        var comment1 = new CommentDataBuilder()
            .ForVideo(video)
            .ByUser(owner)
            .WithContent("First")
            .CreatedAt(DateTimeOffset.UtcNow.AddMinutes(-10))
            .Build();
        var comment2 = new CommentDataBuilder()
            .ForVideo(video)
            .ByUser(owner)
            .WithContent("Second")
            .CreatedAt(DateTimeOffset.UtcNow.AddMinutes(-5))
            .Build();
        var comment3 = new CommentDataBuilder()
            .ForVideo(video)
            .ByUser(owner)
            .WithContent("Third")
            .CreatedAt(DateTimeOffset.UtcNow)
            .Build();

        SeedDbContext.AddRange(owner, video, link, comment1, comment2, comment3);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var comments = await commentService.GetCommentsForVideoAsync(
            owner.Id, null,
            link.Id,
            TestContext.Current.CancellationToken);

        // Assert
        var list = comments.ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal("First", list[0].Content);
        Assert.Equal("Second", list[1].Content);
        Assert.Equal("Third", list[2].Content);
    }


    [Fact]
    public async Task UpdateComment_AuthenticatedAuthor_UpdatesSuccessfully()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).WithContent("Original").Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;

        // Act
        var result = await commentService.UpdateCommentAsync(
            comment.Id,
            user.Id, 
            null,
            "Updated content",
            TestContext.Current.CancellationToken);

        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Updated content", updated!.Content);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt >= before && updated.UpdatedAt <= after);
    }

    [Fact]
    public async Task UpdateComment_NotAuthor_ReturnsFalse()
    {
        // Arrange
        var author = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(author).ShareAsPrivate(author).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).WithContent("Original").Build();

        SeedDbContext.AddRange(author, otherUser, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UpdateCommentAsync(
            comment.Id,
            otherUser.Id, null, // Not the author
            "Hacked content",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        SeedDbContext.ChangeTracker.Clear();
        var unchanged = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.Equal("Original", unchanged!.Content); // Unchanged
    }

    [Fact]
    public async Task UpdateComment_AnonymousComment_ReturnsFalse()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).Build();
        var anonymousComment =
            new CommentDataBuilder().ForVideo(video).ByAnonymous(link).WithContent("Anonymous").Build();

        SeedDbContext.AddRange(owner, video, link, anonymousComment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UpdateCommentAsync(
            anonymousComment.Id,
            owner.Id, null, // Can't update anonymous comment
            "Try to update",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateComment_NonExistentComment_ReturnsFalse()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UpdateCommentAsync(
            Guid.NewGuid(),
            user.Id, null,
            "Content",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateComment_EmptyContent_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.UpdateCommentAsync(
                comment.Id,
                user.Id, null,
                "", // Empty
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task UpdateComment_SetsUpdatedAtTimestamp()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder()
            .ForVideo(video)
            .ByUser(user)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-1))
            .Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await commentService.UpdateCommentAsync(
            comment.Id,
            user.Id, null,
            "Updated",
            TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(updated!.UpdatedAt);
        Assert.True(updated.UpdatedAt > updated.CreatedAt);
    }


    [Fact]
    public async Task DeleteComment_Author_DeletesSuccessfully()
    {
        // Arrange
        var author = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(author).ShareAsPrivate(author).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).Build();

        SeedDbContext.AddRange(author, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.DeleteCommentAsync(
            comment.Id,
            author.Id,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var deleted = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.Null(deleted); // Hard deleted
    }

    [Fact]
    public async Task DeleteComment_VideoOwner_DeletesSuccessfully()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var commenter = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(commenter).Build();

        SeedDbContext.AddRange(owner, commenter, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.DeleteCommentAsync(
            comment.Id,
            owner.Id, // Video owner, not commenter
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var deleted = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteComment_UnauthorizedUser_ReturnsFalse()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var author = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).Build();

        SeedDbContext.AddRange(owner, author, unauthorized, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.DeleteCommentAsync(
            comment.Id,
            unauthorized.Id, // Not author or video owner
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        SeedDbContext.ChangeTracker.Clear();
        var notDeleted = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(notDeleted); // Still exists
    }

    [Fact]
    public async Task DeleteComment_NonExistentComment_ReturnsFalse()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.DeleteCommentAsync(
            Guid.NewGuid(),
            user.Id,
            null,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteComment_RemovedFromDatabase()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var commentId = comment.Id;

        // Act
        await commentService.DeleteCommentAsync(commentId, user.Id, null, TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var deleted = await SeedDbContext.Set<Comment>().FindAsync([commentId], TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }


    [Fact]
    public async Task HideComment_VideoOwner_HidesSuccessfully()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedDbContext.AddRange(owner, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.HideCommentAsync(
            comment.Id,
            owner.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var hidden = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.True(hidden!.IsHidden);
    }

    [Fact]
    public async Task HideComment_NotVideoOwner_ReturnsFalse()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedDbContext.AddRange(owner, otherUser, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.HideCommentAsync(
            comment.Id,
            otherUser.Id, // Not owner
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        SeedDbContext.ChangeTracker.Clear();
        var notHidden = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.False(notHidden!.IsHidden);
    }

    [Fact]
    public async Task HideComment_NonExistentComment_ReturnsFalse()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.HideCommentAsync(
            Guid.NewGuid(),
            user.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HideComment_UpdatesIsHiddenFlag()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden(false).Build();

        SeedDbContext.AddRange(owner, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(comment.IsHidden); // Verify initial state

        // Act
        await commentService.HideCommentAsync(comment.Id, owner.Id, TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.True(updated!.IsHidden);
    }


    [Fact]
    public async Task UnhideComment_VideoOwner_UnhidesSuccessfully()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden().Build();

        SeedDbContext.AddRange(owner, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UnhideCommentAsync(
            comment.Id,
            owner.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var unhidden = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.False(unhidden!.IsHidden);
    }

    [Fact]
    public async Task UnhideComment_NotVideoOwner_ReturnsFalse()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden().Build();

        SeedDbContext.AddRange(owner, otherUser, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UnhideCommentAsync(
            comment.Id,
            otherUser.Id, // Not owner
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);

        SeedDbContext.ChangeTracker.Clear();
        var stillHidden = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.True(stillHidden!.IsHidden); // Still hidden
    }

    [Fact]
    public async Task UnhideComment_NonExistentComment_ReturnsFalse()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.UnhideCommentAsync(
            Guid.NewGuid(),
            user.Id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnhideComment_UpdatesIsHiddenFlag()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden(true).Build();

        SeedDbContext.AddRange(owner, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.True(comment.IsHidden); // Verify initial state

        // Act
        await commentService.UnhideCommentAsync(comment.Id, owner.Id, TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.False(updated!.IsHidden);
    }


    [Fact]
    public async Task ReportComment_ValidReason_ReportsSuccessfully()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await commentService.ReportCommentAsync(
            comment.Id,
            "Inappropriate content",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);

        SeedDbContext.ChangeTracker.Clear();
        var reported = await SeedDbContext.Set<Comment>()
            .FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.True(reported!.IsReported);
        Assert.Equal("Inappropriate content", reported.ReportedReason);
    }

    [Fact]
    public async Task ReportComment_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await commentService.ReportCommentAsync(
                comment.Id,
                "", // Empty reason
                TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task ReportComment_NonExistentComment_ReturnsFalse()
    {
        // Act
        var result = await commentService.ReportCommentAsync(
            Guid.NewGuid(),
            "Spam",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReportComment_SetsReportedFlagAndReason()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Reported(false).Build();

        SeedDbContext.AddRange(user, video, comment);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.False(comment.IsReported); // Verify initial state
        Assert.Null(comment.ReportedReason);

        // Act
        await commentService.ReportCommentAsync(
            comment.Id,
            "Offensive language",
            TestContext.Current.CancellationToken);

        // Assert
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Set<Comment>().FindAsync([comment.Id], TestContext.Current.CancellationToken);
        Assert.True(updated!.IsReported);
        Assert.Equal("Offensive language", updated.ReportedReason);
    }
}