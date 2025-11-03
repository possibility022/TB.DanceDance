using Application.Services;
using Domain.Entities;
using Infrastructure.Data;

namespace TB.DanceDance.Tests.Application;

public class AccessServiceTests : BaseTestClass
{
    private AccessService accessService = null!;

    public AccessServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        accessService = new AccessService(runtimeDbContext);
        return ValueTask.CompletedTask;
    }

    // E1: Can upload to event when assigned
    [Fact]
    public async Task CanUserUploadToEventAsync_ReturnsTrue_WhenAssigned()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        SeedDbContext.AddRange(owner, user, evt, membership);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await accessService.CanUserUploadToEventAsync(user.Id, evt.Id, TestContext.Current.CancellationToken);
        Assert.True(can);
    }

    // E2: Cannot upload to event when not assigned
    [Fact]
    public async Task CanUserUploadToEventAsync_ReturnsFalse_WhenNotAssigned()
    {
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var stranger = new UserDataBuilder().Build();

        SeedDbContext.AddRange(owner, evt, stranger);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await accessService.CanUserUploadToEventAsync(stranger.Id, evt.Id, TestContext.Current.CancellationToken);
        Assert.False(can);
    }

    // G1: Can upload to group when member
    [Fact]
    public async Task CanUserUploadToGroupAsync_ReturnsTrue_WhenAssigned()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var membership = userB.AssignTo(group, DateTime.UtcNow);

        SeedDbContext.AddRange(user, group, membership);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await accessService.CanUserUploadToGroupAsync(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.True(can);
    }

    // G2: Cannot upload to group when not member
    [Fact]
    public async Task CanUserUploadToGroupAsync_ReturnsFalse_WhenNotAssigned()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(user, group);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await accessService.CanUserUploadToGroupAsync(user.Id, group.Id, TestContext.Current.CancellationToken);
        Assert.False(can);
    }

    // A: Returns both events and groups for a user
    [Fact]
    public async Task GetUserEventsAndGroupsAsync_ReturnsUserGroupsAndEvents()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var groupA = new GroupDataBuilder().Build();
        var groupB = new GroupDataBuilder().Build();
        var memA = userB.AssignTo(groupA, DateTime.UtcNow);
        var memB = userB.AssignTo(groupB, DateTime.UtcNow);

        var evtAB = new EventDataBuilder();
        var ownerA = evtAB.BuildOwner();
        var evtA = evtAB.Build();
        var evtBB = new EventDataBuilder();
        var ownerB = evtBB.BuildOwner();
        var evtB = evtBB.Build();
        var partA = userB.AssignTo(evtA);
        var partB = userB.AssignTo(evtB);

        SeedDbContext.AddRange(user, groupA, groupB, memA, memB,
            ownerA, ownerB, evtA, evtB, partA, partB);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (groups, events) = await accessService.GetUserEventsAndGroupsAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, events.Count);
        Assert.Contains(groups, g => g.Id == groupA.Id);
        Assert.Contains(groups, g => g.Id == groupB.Id);
        Assert.Contains(events, e => e.Id == evtA.Id);
        Assert.Contains(events, e => e.Id == evtB.Id);
    }

    // B: Returns empty when user has no memberships
    [Fact]
    public async Task GetUserEventsAndGroupsAsync_ReturnsEmpty_WhenNoMemberships()
    {
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (groups, events) = await accessService.GetUserEventsAndGroupsAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.Empty(groups);
        Assert.Empty(events);
    }

    // C: Throws on null userId
    [Fact]
    public async Task GetUserEventsAndGroupsAsync_Throws_WhenUserIdIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await accessService.GetUserEventsAndGroupsAsync(null!, TestContext.Current.CancellationToken);
        });
    }

    // D: Duplicates are returned when duplicate memberships exist (current behavior)
    [Fact]
    public async Task GetUserEventsAndGroupsAsync_ReturnsDuplicates_WhenDuplicateMembershipsExist()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();

        var m1 = userB.AssignTo(group, DateTime.UtcNow);
        var m2 = userB.AssignTo(group, DateTime.UtcNow.AddMinutes(1));
        var p1 = userB.AssignTo(evt);
        var p2 = userB.AssignTo(evt);

        SeedDbContext.AddRange(user, group, owner, evt, m1, m2, p1, p2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (groups, events) = await accessService.GetUserEventsAndGroupsAsync(user.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, groups.Count); // duplicate groups expected
        Assert.Equal(2, events.Count); // duplicate events expected
        Assert.True(groups.All(g => g.Id == group.Id));
        Assert.True(events.All(e => e.Id == evt.Id));
    }
}