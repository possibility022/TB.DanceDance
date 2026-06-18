using Application.Features.Competitions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Competitions;

public class CompetitionServiceTests : BaseTestClass
{
    private ICompetitionService competitionService = null!;

    public CompetitionServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        competitionService = new CompetitionService(runtimeDbContext);
        return ValueTask.CompletedTask;
    }

    private Video AddOwnedVideo(User owner, string? name = null)
    {
        var video = new VideoDataBuilder().OwnedBy(owner).WithName(name ?? "Video").Build();
        SeedDbContext.Add(video);
        return video;
    }

    private async Task PersistAsync(params object[] entities)
    {
        SeedDbContext.AddRange(entities);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Create_HappyPath_PersistsCompetitionOwnedByUser()
    {
        var owner = new UserDataBuilder().Build();
        await PersistAsync(owner);

        var competition = await competitionService.CreateAsync(
            owner.Id, "Nationals", new DateTime(2026, 5, 1), "Warsaw", CommentVisibility.Public,
            TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, competition.Id);
        Assert.Equal(owner.Id, competition.OwnerUserId);
        Assert.Equal("Nationals", competition.Name);
        Assert.Equal("Warsaw", competition.Location);
        Assert.Equal(CommentVisibility.Public, competition.CommentVisibility);

        SeedDbContext.ChangeTracker.Clear();
        var persisted = await SeedDbContext.Competitions.FindAsync([competition.Id], TestContext.Current.CancellationToken);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task Create_BlankName_Throws()
    {
        var owner = new UserDataBuilder().Build();
        await PersistAsync(owner);

        await Assert.ThrowsAsync<ArgumentException>(() => competitionService.CreateAsync(
            owner.Id, "   ", null, null, CommentVisibility.OwnerOnly, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddVideo_HappyPath_GroupsTheVideo()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = AddOwnedVideo(owner);
        await PersistAsync(owner, competition);

        await competitionService.AddVideoAsync(competition.Id, video.Id, owner.Id, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(competition.Id, reloaded.CompetitionId);
    }

    [Fact]
    public async Task AddVideo_AlreadyInAnotherCompetition_Throws()
    {
        var owner = new UserDataBuilder().Build();
        var first = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var second = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(first).Build();
        await PersistAsync(owner, first, second, video);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => competitionService.AddVideoAsync(
            second.Id, video.Id, owner.Id, TestContext.Current.CancellationToken));
        Assert.Contains("already in another competition", ex.Message);
    }

    [Fact]
    public async Task AddVideo_NonOwnerVideo_Throws()
    {
        var owner = new UserDataBuilder().Build();
        var otherUser = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var foreignVideo = new VideoDataBuilder().OwnedBy(otherUser).Build();
        await PersistAsync(owner, otherUser, competition, foreignVideo);

        await Assert.ThrowsAsync<ArgumentException>(() => competitionService.AddVideoAsync(
            competition.Id, foreignVideo.Id, owner.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddVideo_NonOwnerCompetition_Throws()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var attackerVideo = new VideoDataBuilder().OwnedBy(attacker).Build();
        await PersistAsync(owner, attacker, competition, attackerVideo);

        // The attacker owns the video but not the competition.
        await Assert.ThrowsAsync<ArgumentException>(() => competitionService.AddVideoAsync(
            competition.Id, attackerVideo.Id, attacker.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddVideo_Idempotent_WhenAlreadyInThisCompetition()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        await PersistAsync(owner, competition, video);

        await competitionService.AddVideoAsync(competition.Id, video.Id, owner.Id, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(competition.Id, reloaded.CompetitionId);
    }

    [Fact]
    public async Task RemoveVideo_DetachesVideo()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        await PersistAsync(owner, competition, video);

        var removed = await competitionService.RemoveVideoAsync(competition.Id, video.Id, owner.Id, TestContext.Current.CancellationToken);

        Assert.True(removed);
        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Null(reloaded.CompetitionId);
    }

    [Fact]
    public async Task RemoveVideo_NonOwner_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        await PersistAsync(owner, attacker, competition, video);

        var removed = await competitionService.RemoveVideoAsync(competition.Id, video.Id, attacker.Id, TestContext.Current.CancellationToken);

        Assert.False(removed);
        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(competition.Id, reloaded.CompetitionId);
    }

    [Fact]
    public async Task Rename_HappyPath_And_NonOwnerReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).WithName("Old").Build();
        await PersistAsync(owner, attacker, competition);

        var byAttacker = await competitionService.RenameAsync(competition.Id, attacker.Id, "Hacked", TestContext.Current.CancellationToken);
        Assert.False(byAttacker);

        var byOwner = await competitionService.RenameAsync(competition.Id, owner.Id, "New", TestContext.Current.CancellationToken);
        Assert.True(byOwner);

        SeedDbContext.ChangeTracker.Clear();
        var reloaded = await SeedDbContext.Competitions.FirstAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken);
        Assert.Equal("New", reloaded.Name);
    }

    [Fact]
    public async Task Delete_DetachesVideos_AndRemovesCompetition()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video1 = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        var video2 = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        await PersistAsync(owner, competition, video1, video2);

        var deleted = await competitionService.DeleteAsync(competition.Id, owner.Id, TestContext.Current.CancellationToken);

        Assert.True(deleted);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(await SeedDbContext.Competitions.AnyAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken));
        var videos = await SeedDbContext.Videos
            .Where(v => v.Id == video1.Id || v.Id == video2.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, videos.Count);
        Assert.All(videos, v => Assert.Null(v.CompetitionId));
    }

    [Fact]
    public async Task Delete_NonOwner_ReturnsFalse()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        await PersistAsync(owner, attacker, competition);

        var deleted = await competitionService.DeleteAsync(competition.Id, attacker.Id, TestContext.Current.CancellationToken);

        Assert.False(deleted);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(await SeedDbContext.Competitions.AnyAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ListMyCompetitions_ReturnsOnlyOwnersWithVideos()
    {
        var owner = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var mine1 = new CompetitionDataBuilder().OwnedBy(owner).CreatedAt(DateTime.UtcNow.AddDays(-2)).Build();
        var mine2 = new CompetitionDataBuilder().OwnedBy(owner).CreatedAt(DateTime.UtcNow.AddDays(-1)).Build();
        var theirs = new CompetitionDataBuilder().OwnedBy(other).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(mine1).Build();
        await PersistAsync(owner, other, mine1, mine2, theirs, video);

        var result = await competitionService.ListMyCompetitionsAsync(owner.Id, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, c => c.Id == theirs.Id);
        // Newest first.
        Assert.Equal(mine2.Id, result.First().Id);
        var withVideo = result.First(c => c.Id == mine1.Id);
        Assert.Single(withVideo.Videos);
    }

    [Fact]
    public async Task Get_ReturnsCompetitionWithVideos_NullForNonOwner()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();
        await PersistAsync(owner, attacker, competition, video);

        var asOwner = await competitionService.GetAsync(competition.Id, owner.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(asOwner);
        Assert.Single(asOwner!.Videos);

        var asAttacker = await competitionService.GetAsync(competition.Id, attacker.Id, TestContext.Current.CancellationToken);
        Assert.Null(asAttacker);
    }
}
