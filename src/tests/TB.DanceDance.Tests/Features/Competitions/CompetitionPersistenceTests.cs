using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Competitions;

public class CompetitionPersistenceTests : BaseTestClass
{
    public CompetitionPersistenceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext) => ValueTask.CompletedTask;

    [Fact]
    public async Task Competition_WithVideos_RoundTrips()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder()
            .OwnedBy(owner)
            .WithName("Nationals 2026")
            .At("Warsaw")
            .WithCommentVisibility(CommentVisibility.AuthenticatedOnly)
            .Build();
        var video1 = new VideoDataBuilder().OwnedBy(owner).WithName("Round 1").InCompetition(competition).Build();
        var video2 = new VideoDataBuilder().OwnedBy(owner).WithName("Round 2").InCompetition(competition).Build();

        SeedDbContext.AddRange(owner, competition, video1, video2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act
        var reloaded = await SeedDbContext.Set<Competition>()
            .Include(c => c.Videos)
            .FirstOrDefaultAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(reloaded);
        Assert.Equal(owner.Id, reloaded!.OwnerUserId);
        Assert.Equal("Nationals 2026", reloaded.Name);
        Assert.Equal("Warsaw", reloaded.Location);
        Assert.Equal(CommentVisibility.AuthenticatedOnly, reloaded.CommentVisibility);
        Assert.Equal(2, reloaded.Videos.Count);
        Assert.Contains(reloaded.Videos, v => v.Id == video1.Id);
        Assert.Contains(reloaded.Videos, v => v.Id == video2.Id);
    }

    [Fact]
    public async Task Video_CompetitionId_SetsAndClears()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).Build();

        SeedDbContext.AddRange(owner, competition, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act - attach the video to the competition
        var toAttach = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        toAttach.CompetitionId = competition.Id;
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        var attached = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(competition.Id, attached.CompetitionId);

        // Act - detach it again
        attached.CompetitionId = null;
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Assert
        var detached = await SeedDbContext.Videos.FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Null(detached.CompetitionId);
    }

    [Fact]
    public async Task DeletingCompetition_LeavesVideos_WithCompetitionIdNulled()
    {
        // Arrange
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var video = new VideoDataBuilder().OwnedBy(owner).InCompetition(competition).Build();

        SeedDbContext.AddRange(owner, competition, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Act - delete the competition
        var toDelete = await SeedDbContext.Set<Competition>().FirstAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken);
        SeedDbContext.Remove(toDelete);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // Assert - the video survives, but is now standalone (CompetitionId nulled)
        var survivingVideo = await SeedDbContext.Videos.FirstOrDefaultAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(survivingVideo);
        Assert.Null(survivingVideo!.CompetitionId);

        var competitionGone = await SeedDbContext.Set<Competition>().AnyAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken);
        Assert.False(competitionGone);
    }
}
