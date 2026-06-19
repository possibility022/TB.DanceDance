using System.Security.Claims;
using Application.Features.AccessManagement;
using Application.Features.Sharing;
using Application.Features.Sharing.Endpoints;
using Domain.Entities;
using FastEndpoints;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using TB.DanceDance.API.Contracts.Features.Sharing;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Competitions;

public class CompetitionSharingTests : BaseTestClass
{
    private ISharedLinkService sharedLinkService = null!;
    private ISharedLinkService seedingSharedLinkService = null!;

    public CompetitionSharingTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        sharedLinkService = new SharedLinkService(runtimeDbContext, new AccessService(runtimeDbContext));
        seedingSharedLinkService = new SharedLinkService(SeedDbContext, new AccessService(SeedDbContext));
        return ValueTask.CompletedTask;
    }

    private static DefaultHttpContext Ctx(string? sub, params (string Key, object Value)[] routeValues)
    {
        var ctx = new DefaultHttpContext();
        if (sub != null)
        {
            var identity = new ClaimsIdentity([new Claim("sub", sub)], "test");
            ctx.User = new ClaimsPrincipal(identity);
        }
        foreach (var (key, value) in routeValues)
            ctx.Request.RouteValues[key] = value;
        return ctx;
    }

    // Builds a converted private video owned by the user, optionally grouped into a competition.
    private Video AddVideo(User owner, Competition? competition = null)
    {
        var builder = new VideoDataBuilder().OwnedBy(owner).Converted().WithBlobId($"blob-{Guid.NewGuid():N}").ShareAsPrivate(owner);
        if (competition != null)
            builder.InCompetition(competition);
        var video = builder.Build();
        SeedDbContext.AddRange(video, builder.BuildShares().Single());
        return video;
    }

    [Fact]
    public async Task CreateCompetitionSharedLink_HappyPath_TargetsCompetition()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        AddVideo(owner, competition);
        SeedDbContext.AddRange(owner, competition);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await sharedLinkService.CreateCompetitionSharedLinkAsync(
            competition.Id, owner.Id, 7, allowComments: true, allowAnonymousComments: false,
            TestContext.Current.CancellationToken);

        Assert.Equal(competition.Id, link.CompetitionId);
        Assert.Null(link.VideoId);
        Assert.Equal(owner.Id, link.SharedBy);
    }

    [Fact]
    public async Task CreateCompetitionSharedLink_NonOwner_Throws()
    {
        var owner = new UserDataBuilder().Build();
        var attacker = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        SeedDbContext.AddRange(owner, attacker, competition);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ArgumentException>(() => sharedLinkService.CreateCompetitionSharedLinkAsync(
            competition.Id, attacker.Id, 7, true, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetInfoEndpoint_ForCompetition_Returns200WithEveryVideo()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).WithName("Worlds").Build();
        var v1 = AddVideo(owner, competition);
        var v2 = AddVideo(owner, competition);
        SeedDbContext.AddRange(owner, competition);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingSharedLinkService.CreateCompetitionSharedLinkAsync(
            competition.Id, owner.Id, 7, true, false, TestContext.Current.CancellationToken);

        var ep = Factory.Create<GetVideoInfoBySharedLinkEndpoint>(Ctx(null, ("linkId", link.Id)), sharedLinkService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(200, ep.HttpContext.Response.StatusCode);
        Assert.NotNull(ep.Response);
        Assert.True(ep.Response.IsCompetition);
        Assert.Equal("Worlds", ep.Response.Name);
        Assert.Equal(2, ep.Response.Videos.Count);
        Assert.Contains(ep.Response.Videos, v => v.VideoId == v1.Id);
        Assert.Contains(ep.Response.Videos, v => v.VideoId == v2.Id);
    }

    [Fact]
    public async Task GetVideoForSharedLink_CompetitionLink_ResolvesEachVideo_RejectsForeign()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        var v1 = AddVideo(owner, competition);
        var v2 = AddVideo(owner, competition);
        var standalone = AddVideo(owner); // not in the competition
        SeedDbContext.AddRange(owner, competition);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await sharedLinkService.CreateCompetitionSharedLinkAsync(
            competition.Id, owner.Id, 7, true, false, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        Assert.NotNull(await sharedLinkService.GetVideoForSharedLinkAsync(link.Id, v1.Id, TestContext.Current.CancellationToken));
        Assert.NotNull(await sharedLinkService.GetVideoForSharedLinkAsync(link.Id, v2.Id, TestContext.Current.CancellationToken));
        Assert.Null(await sharedLinkService.GetVideoForSharedLinkAsync(link.Id, standalone.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetVideoForSharedLink_SingleVideoLink_StillWorks()
    {
        var owner = new UserDataBuilder().Build();
        var video = AddVideo(owner);
        var other = AddVideo(owner);
        SeedDbContext.Add(owner);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await sharedLinkService.CreateSharedLinkAsync(
            video.Id, owner.Id, 7, true, false, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        // The single-video link streams its own video, and rejects an unrelated video id.
        Assert.NotNull(await sharedLinkService.GetVideoForSharedLinkAsync(link.Id, video.Id, TestContext.Current.CancellationToken));
        Assert.Null(await sharedLinkService.GetVideoForSharedLinkAsync(link.Id, other.Id, TestContext.Current.CancellationToken));

        // The legacy single-video info path still resolves via the endpoint.
        var ep = Factory.Create<GetVideoInfoBySharedLinkEndpoint>(Ctx(null, ("linkId", link.Id)), sharedLinkService);
        await ep.HandleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(200, ep.HttpContext.Response.StatusCode);
        Assert.NotNull(ep.Response);
        Assert.False(ep.Response.IsCompetition);
        Assert.Equal(video.Id, ep.Response.VideoId);
        Assert.Empty(ep.Response.Videos);
    }

    [Fact]
    public async Task DeletingCompetition_CascadesItsSharedLinks()
    {
        var owner = new UserDataBuilder().Build();
        var competition = new CompetitionDataBuilder().OwnedBy(owner).Build();
        AddVideo(owner, competition);
        SeedDbContext.AddRange(owner, competition);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await sharedLinkService.CreateCompetitionSharedLinkAsync(
            competition.Id, owner.Id, 7, true, false, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        var toDelete = await SeedDbContext.Competitions.FirstAsync(c => c.Id == competition.Id, TestContext.Current.CancellationToken);
        SeedDbContext.Competitions.Remove(toDelete);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        Assert.False(await SeedDbContext.SharedLinks.AnyAsync(l => l.Id == link.Id, TestContext.Current.CancellationToken));
    }
}
