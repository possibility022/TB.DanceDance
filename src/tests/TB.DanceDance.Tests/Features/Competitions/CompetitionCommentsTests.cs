using Application.Features.Comments;
using Domain.Entities;
using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Competitions;

/// <summary>
/// Competition-level comments: one combined thread per competition, keyed off the shared link's
/// competition target rather than a single video. Owner/visibility come from the competition.
/// </summary>
public class CompetitionCommentsTests : BaseTestClass
{
    private ICommentService commentService = null!;

    public CompetitionCommentsTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        commentService = new CommentService(runtimeDbContext, null!);
        return ValueTask.CompletedTask;
    }

    // Tests in this class share one database (per-class connection), so authors get unique ids.
    private sealed record Seeded(User Owner, Competition Competition, SharedLink Link, User Teacher, User Viewer, User Stranger);

    // Owner + competition (given visibility) + two grouped videos + a competition share link,
    // plus distinct teacher/viewer/stranger users (FK on Comment.UserId requires them to exist).
    private async Task<Seeded> SeedCompetition(CommentVisibility visibility, bool allowAnonymous = true)
    {
        var owner = new UserDataBuilder();
        var teacher = new UserDataBuilder();
        var viewer = new UserDataBuilder();
        var stranger = new UserDataBuilder();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).WithCommentVisibility(visibility).Build();
        var v1 = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        var v2 = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        var linkBuilder = new SharedLinkDataBuilder().ForCompetition(competition).SharedBy(owner);
        if (allowAnonymous) linkBuilder.AllowAnonymousComments(); else linkBuilder.AllowComments();
        var link = linkBuilder.Build();

        SeedDbContext.AddRange(owner.Build(), teacher.Build(), viewer.Build(), stranger.Build(),
            competition, v1, v2, link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        return new Seeded(owner.Build(), competition, link, teacher.Build(), viewer.Build(), stranger.Build());
    }

    [Fact]
    public async Task CreateComment_OnCompetitionLink_KeysOffCompetition()
    {
        var s = await SeedCompetition(CommentVisibility.Public);

        var comment = await commentService.CreateCommentAsync(
            s.Teacher.Id, s.Link.Id, "Great competition!", null, null, TestContext.Current.CancellationToken);

        Assert.Equal(s.Competition.Id, comment.CompetitionId);
        Assert.Null(comment.VideoId);
        Assert.Equal(s.Teacher.Id, comment.UserId);
        Assert.Equal(s.Link.Id, comment.SharedLinkId);
    }

    [Fact]
    public async Task CreateComment_AnonymousOnCompetitionLink_Works()
    {
        var s = await SeedCompetition(CommentVisibility.Public);

        var comment = await commentService.CreateCommentAsync(
            null, s.Link.Id, "Nice!", "Coach", "anon-secret-1", TestContext.Current.CancellationToken);

        Assert.Equal(s.Competition.Id, comment.CompetitionId);
        Assert.Null(comment.VideoId);
        Assert.True(comment.PostedAsAnonymous);
        Assert.NotNull(comment.ShaOfAnonymousId);
    }

    [Fact]
    public async Task CombinedThread_OwnerSeesAllCommentsAcrossTheCompetition()
    {
        var s = await SeedCompetition(CommentVisibility.OwnerOnly);

        await commentService.CreateCommentAsync(s.Teacher.Id, s.Link.Id, "Feedback one", null, null, TestContext.Current.CancellationToken);
        await commentService.CreateCommentAsync(null, s.Link.Id, "Feedback two", "Coach", "anon-1", TestContext.Current.CancellationToken);

        var (comments, total) = await commentService.GetCommentsForVideoAsync(
            s.Owner.Id, null, s.Link.Id, 1, 50, TestContext.Current.CancellationToken);

        Assert.Equal(2, total);
        Assert.Equal(2, comments.Count);
    }

    [Theory]
    [InlineData(CommentVisibility.Public)]
    [InlineData(CommentVisibility.AuthenticatedOnly)]
    public async Task CombinedThread_NonOwnerSeesPostedComments_PerVisibility(CommentVisibility visibility)
    {
        var s = await SeedCompetition(visibility);

        await commentService.CreateCommentAsync(s.Teacher.Id, s.Link.Id, "Visible feedback", null, null, TestContext.Current.CancellationToken);

        // Another authenticated viewer (not the owner) reads the thread.
        var (comments, total) = await commentService.GetCommentsForVideoAsync(
            s.Viewer.Id, null, s.Link.Id, 1, 50, TestContext.Current.CancellationToken);

        Assert.Equal(1, total);
        Assert.Single(comments);
    }

    [Fact]
    public async Task CombinedThread_OwnerOnlyVisibility_HidesOthersCommentsFromAnonymousViewer()
    {
        var s = await SeedCompetition(CommentVisibility.OwnerOnly);

        // Teacher posts; a different anonymous viewer should not see it.
        await commentService.CreateCommentAsync(s.Teacher.Id, s.Link.Id, "Private to owner", null, null, TestContext.Current.CancellationToken);

        var (comments, total) = await commentService.GetCommentsForVideoAsync(
            null, "some-other-anon", s.Link.Id, 1, 50, TestContext.Current.CancellationToken);

        Assert.Equal(0, total);
        Assert.Empty(comments);
    }

    [Fact]
    public async Task HideComment_ByCompetitionOwner_Works_ByNonOwner_Fails()
    {
        var s = await SeedCompetition(CommentVisibility.Public);
        var comment = await commentService.CreateCommentAsync(s.Teacher.Id, s.Link.Id, "to hide", null, null, TestContext.Current.CancellationToken);

        var byStranger = await commentService.HideCommentAsync(comment.Id, s.Stranger.Id, TestContext.Current.CancellationToken);
        Assert.False(byStranger);

        var byOwner = await commentService.HideCommentAsync(comment.Id, s.Owner.Id, TestContext.Current.CancellationToken);
        Assert.True(byOwner);

        // Hidden comment is not visible to a public non-owner viewer, but the owner still sees it.
        var (publicView, publicTotal) = await commentService.GetCommentsForVideoAsync(
            s.Viewer.Id, null, s.Link.Id, 1, 50, TestContext.Current.CancellationToken);
        Assert.Equal(0, publicTotal);
        Assert.Empty(publicView);

        var (ownerView, ownerTotal) = await commentService.GetCommentsForVideoAsync(
            s.Owner.Id, null, s.Link.Id, 1, 50, TestContext.Current.CancellationToken);
        Assert.Equal(1, ownerTotal);
        Assert.Single(ownerView);
    }

    [Fact]
    public async Task ReportComment_OnCompetitionComment_Works()
    {
        var s = await SeedCompetition(CommentVisibility.Public);
        var comment = await commentService.CreateCommentAsync(s.Teacher.Id, s.Link.Id, "to report", null, null, TestContext.Current.CancellationToken);

        var reported = await commentService.ReportCommentAsync(comment.Id, "spam", TestContext.Current.CancellationToken);

        Assert.True(reported);
    }

    [Fact]
    public async Task PerVideoThread_IsUnaffectedByCompetitionThread()
    {
        // A standalone video with its own link/thread, plus a separate competition with its own thread.
        var owner = new UserDataBuilder().Build();
        var author = new UserDataBuilder().WithId("t").Build();
        var standalone = new VideoDataBuilder().OwnedBy(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        var videoLink = new SharedLinkDataBuilder().ForVideo(standalone).SharedBy(owner).AllowAnonymousComments().Build();

        var competition = new CompetitionDataBuilder().OwnedBy(owner).WithCommentVisibility(CommentVisibility.Public).Build();
        var compVideo = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        var compLink = new SharedLinkDataBuilder().ForCompetition(competition).SharedBy(owner).AllowAnonymousComments().Build();

        SeedDbContext.AddRange(owner, author, standalone, videoLink, competition, compVideo, compLink);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        await commentService.CreateCommentAsync("t", videoLink.Id, "on the video", null, null, TestContext.Current.CancellationToken);
        await commentService.CreateCommentAsync("t", compLink.Id, "on the competition", null, null, TestContext.Current.CancellationToken);

        var (videoComments, videoTotal) = await commentService.GetCommentsForVideoAsync(
            owner.Id, null, videoLink.Id, 1, 50, TestContext.Current.CancellationToken);
        var (compComments, compTotal) = await commentService.GetCommentsForVideoAsync(
            owner.Id, null, compLink.Id, 1, 50, TestContext.Current.CancellationToken);

        Assert.Equal(1, videoTotal);
        Assert.Equal("on the video", videoComments.Single().Content);
        Assert.Equal(1, compTotal);
        Assert.Equal("on the competition", compComments.Single().Content);
    }
}
