using Application.Features.Groups;
using Application.Features.Invites;
using Application.Features.Videos;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Invites;

public class InviteLinkServiceTests : BaseTestClass
{
    private IInviteLinkService inviteLinkService = null!;
    private IInviteLinkService seedingInviteLinkService = null!;

    public InviteLinkServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        var groupService = new GroupService(runtimeDbContext, Substitute.For<IThumbnailUrlService>());
        inviteLinkService = new InviteLinkService(runtimeDbContext, groupService);
        seedingInviteLinkService = new InviteLinkService(
            SeedDbContext, new GroupService(SeedDbContext, Substitute.For<IThumbnailUrlService>()));
        return ValueTask.CompletedTask;
    }

    private (User admin, Group group) SeedGroupWithAdmin()
    {
        var admin = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin, group);
        SeedDbContext.GroupsAdmins.Add(new GroupAdmin { Id = Guid.NewGuid(), GroupId = group.Id, UserId = admin.Id });
        return (admin, group);
    }

    private (User owner, Event evt) SeedEventWithOwner()
    {
        var owner = new UserDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(owner).Build();
        SeedDbContext.AddRange(owner, evt);
        return (owner, evt);
    }

    [Fact]
    public async Task CreateForGroupAsync_Admin_Succeeds()
    {
        var (admin, group) = SeedGroupWithAdmin();
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await inviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        Assert.Equal(8, link.Id.Length);
        Assert.Equal(group.Id, link.GroupId);
        Assert.Null(link.EventId);
        Assert.Equal(admin.Id, link.CreatedBy);
        Assert.Equal(InviteLinkStatus.Active, link.Status);
        Assert.Equal(InviteLink.ExpirationDays, (link.ExpireAt - link.CreatedAt).Days);
    }

    [Fact]
    public async Task CreateForGroupAsync_NonAdmin_Throws()
    {
        var (_, group) = SeedGroupWithAdmin();
        var other = new UserDataBuilder().Build();
        SeedDbContext.Add(other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            inviteLinkService.CreateForGroupAsync(group.Id, other.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateForEventAsync_Owner_Succeeds()
    {
        var (owner, evt) = SeedEventWithOwner();
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await inviteLinkService.CreateForEventAsync(evt.Id, owner.Id, TestContext.Current.CancellationToken);

        Assert.Equal(evt.Id, link.EventId);
        Assert.Null(link.GroupId);
        Assert.Equal(InviteLinkStatus.Active, link.Status);
    }

    [Fact]
    public async Task CreateForEventAsync_NonOwner_Throws()
    {
        var (_, evt) = SeedEventWithOwner();
        var other = new UserDataBuilder().Build();
        SeedDbContext.Add(other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            inviteLinkService.CreateForEventAsync(evt.Id, other.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RedeemAsync_Group_GrantsMembership()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var redeemer = new UserDataBuilder().Build();
        SeedDbContext.Add(redeemer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RedeemAsync(link.Id, redeemer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.Redeemed, result);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(await SeedDbContext.AssingedToGroups.AnyAsync(
            a => a.GroupId == group.Id && a.UserId == redeemer.Id, TestContext.Current.CancellationToken));
        var saved = await SeedDbContext.InviteLinks.FirstAsync(l => l.Id == link.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InviteLinkStatus.Redeemed, saved.Status);
        Assert.Equal(redeemer.Id, saved.RedeemedByUserId);
        Assert.NotNull(saved.RedeemedAt);
    }

    [Fact]
    public async Task RedeemAsync_Event_GrantsAccess()
    {
        var (owner, evt) = SeedEventWithOwner();
        var redeemer = new UserDataBuilder().Build();
        SeedDbContext.Add(redeemer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForEventAsync(evt.Id, owner.Id, TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RedeemAsync(link.Id, redeemer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.Redeemed, result);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(await SeedDbContext.AssingedToEvents.AnyAsync(
            a => a.EventId == evt.Id && a.UserId == redeemer.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RedeemAsync_AlreadyMember_IsNoOpAndLinkStaysActive()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var member = new UserDataBuilder().Build();
        SeedDbContext.Add(member);
        SeedDbContext.AssingedToGroups.Add(new AssignedToGroup
        {
            Id = Guid.NewGuid(), GroupId = group.Id, UserId = member.Id, WhenJoined = DateTime.UtcNow,
        });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RedeemAsync(link.Id, member.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.AlreadyMember, result);
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.InviteLinks.FirstAsync(l => l.Id == link.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InviteLinkStatus.Active, saved.Status);

        // A different person can still redeem the still-active link afterward.
        var other = new UserDataBuilder().Build();
        SeedDbContext.Add(other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var secondResult = await inviteLinkService.RedeemAsync(link.Id, other.Id, TestContext.Current.CancellationToken);
        Assert.Equal(RedeemInviteLinkResult.Redeemed, secondResult);
    }

    [Fact]
    public async Task RedeemAsync_SecondRedemptionByDifferentUser_Rejected()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var firstRedeemer = new UserDataBuilder().Build();
        var secondRedeemer = new UserDataBuilder().Build();
        SeedDbContext.AddRange(firstRedeemer, secondRedeemer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        var first = await inviteLinkService.RedeemAsync(link.Id, firstRedeemer.Id, TestContext.Current.CancellationToken);
        var second = await inviteLinkService.RedeemAsync(link.Id, secondRedeemer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.Redeemed, first);
        Assert.Equal(RedeemInviteLinkResult.NotAvailable, second);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(await SeedDbContext.AssingedToGroups.AnyAsync(
            a => a.GroupId == group.Id && a.UserId == secondRedeemer.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RedeemAsync_SamePersonReopeningAfterOwnRedemption_Rejected()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var redeemer = new UserDataBuilder().Build();
        SeedDbContext.Add(redeemer);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        await inviteLinkService.RedeemAsync(link.Id, redeemer.Id, TestContext.Current.CancellationToken);
        var second = await inviteLinkService.RedeemAsync(link.Id, redeemer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.NotAvailable, second);
    }

    [Fact]
    public async Task RedeemAsync_Concurrent_ExactlyOneWinner()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var userA = new UserDataBuilder().Build();
        var userB = new UserDataBuilder().Build();
        SeedDbContext.AddRange(userA, userB);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        // Two independent service instances over independent DbContexts racing for the same row,
        // mirroring two concurrent HTTP requests.
        var ct = TestContext.Current.CancellationToken;
        var taskA = Task.Run(() => inviteLinkService.RedeemAsync(link.Id, userA.Id, ct), ct);
        var taskB = Task.Run(() => seedingInviteLinkService.RedeemAsync(link.Id, userB.Id, ct), ct);
        var results = await Task.WhenAll(taskA, taskB);

        Assert.Equal(1, results.Count(r => r == RedeemInviteLinkResult.Redeemed));
        Assert.Equal(1, results.Count(r => r == RedeemInviteLinkResult.NotAvailable));

        SeedDbContext.ChangeTracker.Clear();
        var memberCount = await SeedDbContext.AssingedToGroups.CountAsync(
            a => a.GroupId == group.Id && (a.UserId == userA.Id || a.UserId == userB.Id), ct);
        Assert.Equal(1, memberCount);
    }

    [Fact]
    public async Task RedeemAsync_ExpiredLink_Rejected()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var redeemer = new UserDataBuilder().Build();
        SeedDbContext.Add(redeemer);
        var link = new InviteLinkDataBuilder()
            .ForGroup(group)
            .CreatedBy(admin)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-10))
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-3))
            .Build();
        SeedDbContext.Add(link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RedeemAsync(link.Id, redeemer.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RedeemInviteLinkResult.NotAvailable, result);
    }

    [Fact]
    public async Task GetInfoAsync_ExpiredLink_IsNotRedeemable()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var link = new InviteLinkDataBuilder()
            .ForGroup(group)
            .CreatedBy(admin)
            .CreatedAt(DateTimeOffset.UtcNow.AddDays(-10))
            .ExpiresAt(DateTimeOffset.UtcNow.AddDays(-3))
            .Build();
        SeedDbContext.Add(link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var info = await inviteLinkService.GetInfoAsync(link.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.False(info!.IsRedeemable);
        Assert.Equal("Group", info.TargetType);
        Assert.Equal(group.Name, info.TargetName);
    }

    [Fact]
    public async Task ListForGroupAsync_AnyCurrentAdmin_SeesLinksRegardlessOfCreator()
    {
        var (firstAdmin, group) = SeedGroupWithAdmin();
        var secondAdmin = new UserDataBuilder().Build();
        SeedDbContext.Add(secondAdmin);
        SeedDbContext.GroupsAdmins.Add(new GroupAdmin { Id = Guid.NewGuid(), GroupId = group.Id, UserId = secondAdmin.Id });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, firstAdmin.Id, TestContext.Current.CancellationToken);

        var links = await inviteLinkService.ListForGroupAsync(group.Id, secondAdmin.Id, TestContext.Current.CancellationToken);

        Assert.Single(links);
        Assert.Equal(link.Id, links.Single().Id);
    }

    [Fact]
    public async Task ListForGroupAsync_NonAdmin_Throws()
    {
        var (_, group) = SeedGroupWithAdmin();
        var other = new UserDataBuilder().Build();
        SeedDbContext.Add(other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            inviteLinkService.ListForGroupAsync(group.Id, other.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RevokeAsync_AnyCurrentAdmin_Succeeds()
    {
        var (firstAdmin, group) = SeedGroupWithAdmin();
        var secondAdmin = new UserDataBuilder().Build();
        SeedDbContext.Add(secondAdmin);
        SeedDbContext.GroupsAdmins.Add(new GroupAdmin { Id = Guid.NewGuid(), GroupId = group.Id, UserId = secondAdmin.Id });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, firstAdmin.Id, TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RevokeAsync(link.Id, secondAdmin.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RevokeInviteLinkResult.Revoked, result);
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.InviteLinks.FirstAsync(l => l.Id == link.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InviteLinkStatus.Revoked, saved.Status);
    }

    [Fact]
    public async Task RevokeAsync_AlreadyRedeemed_NoOp()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var redeemer = new UserDataBuilder().Build();
        SeedDbContext.Add(redeemer);
        var link = new InviteLinkDataBuilder().ForGroup(group).CreatedBy(admin).RedeemedBy(redeemer).Build();
        SeedDbContext.Add(link);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RevokeAsync(link.Id, admin.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RevokeInviteLinkResult.AlreadyRedeemed, result);
        SeedDbContext.ChangeTracker.Clear();
        var saved = await SeedDbContext.InviteLinks.FirstAsync(l => l.Id == link.Id, TestContext.Current.CancellationToken);
        Assert.Equal(InviteLinkStatus.Redeemed, saved.Status);
    }

    [Fact]
    public async Task RevokeAsync_NonAdmin_Denied()
    {
        var (admin, group) = SeedGroupWithAdmin();
        var other = new UserDataBuilder().Build();
        SeedDbContext.Add(other);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var link = await seedingInviteLinkService.CreateForGroupAsync(group.Id, admin.Id, TestContext.Current.CancellationToken);

        var result = await inviteLinkService.RevokeAsync(link.Id, other.Id, TestContext.Current.CancellationToken);

        Assert.Equal(RevokeInviteLinkResult.NotAuthorized, result);
    }

    [Fact]
    public async Task RevokeAsync_NotFound_ReturnsNotFound()
    {
        var result = await inviteLinkService.RevokeAsync("nope1234", TestDataBuilder.RandomUserId(), TestContext.Current.CancellationToken);

        Assert.Equal(RevokeInviteLinkResult.NotFound, result);
    }
}
