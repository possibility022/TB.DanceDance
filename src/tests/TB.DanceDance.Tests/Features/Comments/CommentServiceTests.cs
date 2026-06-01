using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain;
using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Tests.Features.Comments;

/// <summary>
/// Comments feature (Videos module <c>CommentHandlers</c>), driven through the mediator. Note the
/// new <see cref="CommentDto"/> does not carry <c>SharedLinkId</c> or resolved user details (those are
/// edge concerns), so the few assertions that needed them read the persisted entity / assert on UserId.
/// </summary>
public class CommentServiceTests : BaseTestClass
{
    public CommentServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    private Task<Comment?> GetComment(Guid id) =>
        SeedVideosContext.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, TestContext.Current.CancellationToken);

    [Fact]
    public async Task CreateCommentAsync_AuthenticatedUser_CreatesComment()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comment = await Send(new CreateCommentCommand(user.Id, link.Id, "This is a test comment", null, null),
            TestContext.Current.CancellationToken);

        Assert.NotNull(comment);
        Assert.Null(comment.ShaOfAnonymousId);
        Assert.False(comment.PostedAsAnonymous);
        Assert.Null(comment.AnonymousName);
        Assert.Equal(video.Id, comment.VideoId);
        Assert.Equal(user.Id, comment.UserId);
        Assert.Equal("This is a test comment", comment.Content);
        Assert.False(comment.IsHidden);
        Assert.False(comment.IsReported);

        var saved = await GetComment(comment.Id);
        Assert.Equal(link.Id, saved!.SharedLinkId);
    }

    [Fact]
    public async Task CreateCommentAsync_AnonymousUser_CreatesCommentWithLinkId()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowAnonymousComments().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comment = await Send(new CreateCommentCommand(null, link.Id, "Anonymous comment", "Author", "ajdkajwhkdakjgksjdawdgakwdkasjgkejjke"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(comment);
        Assert.NotNull(comment.ShaOfAnonymousId);
        Assert.NotEmpty(comment.ShaOfAnonymousId);
        Assert.NotNull(comment.AnonymousName);
        Assert.NotEmpty(comment.AnonymousName);
        Assert.True(comment.PostedAsAnonymous);
        Assert.Equal(video.Id, comment.VideoId);
        Assert.Null(comment.UserId);
        Assert.Equal("Anonymous comment", comment.Content);

        var saved = await GetComment(comment.Id);
        Assert.Equal(link.Id, saved!.SharedLinkId);
    }

    [Fact]
    public async Task CreateCommentAsync_AuthenticatedUser_DoesNotStoreLinkId()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comment = await Send(new CreateCommentCommand(user.Id, link.Id, "Authenticated comment", null, null),
            TestContext.Current.CancellationToken);

        Assert.Equal(user.Id, comment.UserId);
        var saved = await GetComment(comment.Id);
        Assert.Equal(link.Id, saved!.SharedLinkId);
    }

    [Fact]
    public async Task CreateCommentAsync_LinkDoesNotAllowComments_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments(false).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, link.Id, "Should fail", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_AnonymousUser_LinkDoesNotAllowAnonymous_ThrowsArgumentException()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(null, link.Id, "Should fail", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_ExpiredLink_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user)
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1)).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, link.Id, "Should fail", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_RevokedLink_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).Revoked().AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, link.Id, "Should fail", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_NonExistentLink_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, "notexist", "Should fail", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_EmptyContent_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, link.Id, "", null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_ContentTooLong_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new CreateCommentCommand(user.Id, link.Id, new string('a', 2001), null, null), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateCommentAsync_ValidContent_SetsTimestamps()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;
        var comment = await Send(new CreateCommentCommand(user.Id, link.Id, "Test", null, null), TestContext.Current.CancellationToken);
        var after = DateTimeOffset.UtcNow;

        Assert.True(comment.CreatedAt >= before && comment.CreatedAt <= after);
        Assert.Null(comment.UpdatedAt);
    }

    [Fact]
    public async Task CreateCommentAsync_PersistedInDatabase()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(user).AllowComments().Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comment = await Send(new CreateCommentCommand(user.Id, link.Id, "Persisted comment", null, null), TestContext.Current.CancellationToken);

        var saved = await GetComment(comment.Id);
        Assert.NotNull(saved);
        Assert.Equal("Persisted comment", saved!.Content);
    }

    [Fact]
    public async Task GetComments_VideoOwner_SeesAllComments()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowAnonymousComments().Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment1, comment2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, comments.Count);
        Assert.Contains(comments, c => c.Content == "Visible");
        Assert.Contains(comments, c => c.Content == "Hidden");
    }

    [Fact]
    public async Task GetComments_Public_Anonymous_SeesNonHidden()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowAnonymousComments().Build();

        var visible = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var hidden = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, visible, hidden);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(null, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal(visible.Id, comments.First().Id);
    }

    [Fact]
    public async Task GetComments_Public_Authenticated_SeesNonHidden()
    {
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedAccessContext.AddRange(owner, viewer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment1, comment2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(viewer.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal("Visible", comments.First().Content);
    }

    [Fact]
    public async Task GetComments_AuthenticatedOnly_Anonymous_SeesOnlyHisOwn()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.AuthenticatedOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowAnonymousComments().Build();

        var visible = new CommentDataBuilder().ForVideo(video).WithAnonymousId("1234567890").WithAnonymousName("Anonymous User").WithContent("Own anon comment").Build();
        var notVisible = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Invisible to anon").Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, notVisible, visible);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(null, "1234567890", link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal(visible.Id, comments.First().Id);
    }

    [Fact]
    public async Task GetComments_AuthenticatedOnly_Authenticated_SeesNonHidden()
    {
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.AuthenticatedOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Visible to auth").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedAccessContext.AddRange(owner, viewer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment1, comment2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(viewer.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal("Visible to auth", comments.First().Content);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Anonymous_SeesOnlyHisOwn()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.OwnerOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowAnonymousComments().Build();

        var ownerComment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Owner only").Build();
        var visible = new CommentDataBuilder().ForVideo(video).WithAnonymousId("dahkwjdha").WithAnonymousName("Anonymous User").WithContent("Own anon comment").Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, ownerComment, visible);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(null, "dahkwjdha", link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal(visible.Id, comments.First().Id);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Authenticated_SeesOnlyHisOwn()
    {
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.OwnerOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var ownerComment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Owner only").Build();
        var visible = new CommentDataBuilder().ForVideo(video).ByUser(viewer).WithContent("Viewer's own comment").Build();

        SeedAccessContext.AddRange(owner, viewer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, ownerComment, visible);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(viewer.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Single(comments);
        Assert.Equal(visible.Id, comments.First().Id);
    }

    [Fact]
    public async Task GetComments_OwnerOnly_Owner_SeesAll()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).WithCommentVisibility(CommentVisibility.OwnerOnly).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Comment 1").Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Comment 2").Hidden().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment1, comment2);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, comments.Count);
    }

    [Fact]
    public async Task GetComments_HiddenComments_OnlyVisibleToOwner()
    {
        var owner = new UserDataBuilder().Build();
        var viewer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var hiddenComment = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Hidden").Hidden().Build();

        SeedAccessContext.AddRange(owner, viewer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, hiddenComment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var viewerComments = await Send(new GetCommentsForVideoByLinkQuery(viewer.Id, null, link.Id), TestContext.Current.CancellationToken);
        var ownerComments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);

        Assert.Empty(viewerComments);
        Assert.Single(ownerComments);
    }

    [Fact]
    public async Task GetComments_ExpiredLink_ReturnsEmpty()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).ExpiresAt(DateTimeOffset.UtcNow.AddDays(-1)).AllowComments().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_RevokedLink_ReturnsEmpty()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).Revoked().AllowComments().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_NoComments_ReturnsEmpty()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task GetComments_IncludesUserId_ForAuthenticatedComments()
    {
        // The new CommentDto carries UserId; resolving the author's display name is an API-edge
        // concern (the Videos module cannot navigate to the Access User entity).
        var owner = new UserDataBuilder().WithFirstName("John").WithLastName("Doe").Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);

        var returnedComment = comments.First();
        Assert.Equal(owner.Id, returnedComment.UserId);
        Assert.Equal(owner.Id, returnedComment.VideoOwnerId);
    }

    [Fact]
    public async Task GetComments_OrderedByCreatedAt_Ascending()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).AllowComments().Build();

        var comment1 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("First").CreatedAt(DateTimeOffset.UtcNow.AddMinutes(-10)).Build();
        var comment2 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Second").CreatedAt(DateTimeOffset.UtcNow.AddMinutes(-5)).Build();
        var comment3 = new CommentDataBuilder().ForVideo(video).ByUser(owner).WithContent("Third").CreatedAt(DateTimeOffset.UtcNow).Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, comment1, comment2, comment3);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var comments = await Send(new GetCommentsForVideoByLinkQuery(owner.Id, null, link.Id), TestContext.Current.CancellationToken);

        var list = comments.ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal("First", list[0].Content);
        Assert.Equal("Second", list[1].Content);
        Assert.Equal("Third", list[2].Content);
    }

    [Fact]
    public async Task UpdateComment_AuthenticatedAuthor_UpdatesSuccessfully()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).WithContent("Original").Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var before = DateTimeOffset.UtcNow;
        var result = await Send(new UpdateCommentCommand(comment.Id, user.Id, null, null, "Updated content"), TestContext.Current.CancellationToken);
        var after = DateTimeOffset.UtcNow;

        Assert.True(result);
        var updated = await GetComment(comment.Id);
        Assert.Equal("Updated content", updated!.Content);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt >= before && updated.UpdatedAt <= after);
    }

    [Fact]
    public async Task UpdateComment_NotAuthor_ReturnsFalse()
    {
        var author = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(author).ShareAsPrivate(author).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).WithContent("Original").Build();

        SeedAccessContext.AddRange(author, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentCommand(comment.Id, otherUser.Id, null, null, "Hacked content"), TestContext.Current.CancellationToken);

        Assert.False(result);
        var unchanged = await GetComment(comment.Id);
        Assert.Equal("Original", unchanged!.Content);
    }

    [Fact]
    public async Task UpdateComment_AnonymousComment_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var link = new SharedLinkDataBuilder().ForVideo(video).SharedBy(owner).Build();
        var anonymousComment = new CommentDataBuilder().ForVideo(video).ByAnonymous(link).WithContent("Anonymous").Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, link, anonymousComment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentCommand(anonymousComment.Id, owner.Id, null, null, "Try to update"), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateComment_NonExistentComment_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UpdateCommentCommand(Guid.NewGuid(), user.Id, null, null, "Content"), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateComment_EmptyContent_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new UpdateCommentCommand(comment.Id, user.Id, null, null, ""), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateComment_SetsUpdatedAtTimestamp()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).CreatedAt(DateTimeOffset.UtcNow.AddDays(-1)).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new UpdateCommentCommand(comment.Id, user.Id, null, null, "Updated"), TestContext.Current.CancellationToken);

        var updated = await GetComment(comment.Id);
        Assert.NotNull(updated!.UpdatedAt);
        Assert.True(updated.UpdatedAt > updated.CreatedAt);
    }

    [Fact]
    public async Task DeleteComment_Author_DeletesSuccessfully()
    {
        var author = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(author).ShareAsPrivate(author).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).Build();

        SeedAccessContext.Add(author);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DeleteCommentCommand(comment.Id, author.Id, null), TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Null(await GetComment(comment.Id));
    }

    [Fact]
    public async Task DeleteComment_VideoOwner_DeletesSuccessfully()
    {
        var owner = new UserDataBuilder().Build();
        var commenter = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(commenter).Build();

        SeedAccessContext.AddRange(owner, commenter);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DeleteCommentCommand(comment.Id, owner.Id, null), TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Null(await GetComment(comment.Id));
    }

    [Fact]
    public async Task DeleteComment_UnauthorizedUser_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var author = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(author).Build();

        SeedAccessContext.AddRange(owner, author, unauthorized);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DeleteCommentCommand(comment.Id, unauthorized.Id, null), TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.NotNull(await GetComment(comment.Id));
    }

    [Fact]
    public async Task DeleteComment_NonExistentComment_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DeleteCommentCommand(Guid.NewGuid(), user.Id, null), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteComment_RemovedFromDatabase()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new DeleteCommentCommand(comment.Id, user.Id, null), TestContext.Current.CancellationToken);

        Assert.Null(await GetComment(comment.Id));
    }

    [Fact]
    public async Task HideComment_VideoOwner_HidesSuccessfully()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new HideCommentCommand(comment.Id, owner.Id), TestContext.Current.CancellationToken);

        Assert.True(result);
        var hidden = await GetComment(comment.Id);
        Assert.True(hidden!.IsHidden);
    }

    [Fact]
    public async Task HideComment_NotVideoOwner_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Build();

        SeedAccessContext.AddRange(owner, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new HideCommentCommand(comment.Id, otherUser.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
        var notHidden = await GetComment(comment.Id);
        Assert.False(notHidden!.IsHidden);
    }

    [Fact]
    public async Task HideComment_NonExistentComment_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new HideCommentCommand(Guid.NewGuid(), user.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task HideComment_UpdatesIsHiddenFlag()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden(false).Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new HideCommentCommand(comment.Id, owner.Id), TestContext.Current.CancellationToken);

        var updated = await GetComment(comment.Id);
        Assert.True(updated!.IsHidden);
    }

    [Fact]
    public async Task UnhideComment_VideoOwner_UnhidesSuccessfully()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden().Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UnhideCommentCommand(comment.Id, owner.Id), TestContext.Current.CancellationToken);

        Assert.True(result);
        var unhidden = await GetComment(comment.Id);
        Assert.False(unhidden!.IsHidden);
    }

    [Fact]
    public async Task UnhideComment_NotVideoOwner_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden().Build();

        SeedAccessContext.AddRange(owner, otherUser);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UnhideCommentCommand(comment.Id, otherUser.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
        var stillHidden = await GetComment(comment.Id);
        Assert.True(stillHidden!.IsHidden);
    }

    [Fact]
    public async Task UnhideComment_NonExistentComment_ReturnsFalse()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new UnhideCommentCommand(Guid.NewGuid(), user.Id), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task UnhideComment_UpdatesIsHiddenFlag()
    {
        var owner = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(owner).ShareAsPrivate(owner).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(owner).Hidden(true).Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new UnhideCommentCommand(comment.Id, owner.Id), TestContext.Current.CancellationToken);

        var updated = await GetComment(comment.Id);
        Assert.False(updated!.IsHidden);
    }

    [Fact]
    public async Task ReportComment_ValidReason_ReportsSuccessfully()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ReportCommentCommand(comment.Id, "Inappropriate content"), TestContext.Current.CancellationToken);

        Assert.True(result);
        var reported = await GetComment(comment.Id);
        Assert.True(reported!.IsReported);
        Assert.Equal("Inappropriate content", reported.ReportedReason);
    }

    [Fact]
    public async Task ReportComment_EmptyReason_ThrowsArgumentException()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Send(new ReportCommentCommand(comment.Id, ""), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ReportComment_NonExistentComment_ReturnsFalse()
    {
        var result = await Send(new ReportCommentCommand(Guid.NewGuid(), "Spam"), TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task ReportComment_SetsReportedFlagAndReason()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var comment = new CommentDataBuilder().ForVideo(video).ByUser(user).Reported(false).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(video, comment);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new ReportCommentCommand(comment.Id, "Offensive language"), TestContext.Current.CancellationToken);

        var updated = await GetComment(comment.Id);
        Assert.True(updated!.IsReported);
        Assert.Equal("Offensive language", updated.ReportedReason);
    }
}
