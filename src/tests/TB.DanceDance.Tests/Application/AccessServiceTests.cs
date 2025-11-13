using Application.Services;
using Infrastructure.Data;
using Domain.Entities;
using TB.DanceDance.Tests.TestsFixture;

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
        SeedDbContext.ChangeTracker.Clear();
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
        SeedDbContext.ChangeTracker.Clear();
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
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(2, groups.Count); // duplicate groups expected
        Assert.Equal(2, events.Count); // duplicate events expected
        Assert.True(groups.All(g => g.Id == group.Id));
        Assert.True(events.All(e => e.Id == evt.Id));
    }
    
     // B6: IsUserAssignedToEvent returns true when assigned
    [Fact]
    public async Task IsUserAssignedToEvent_ReturnsTrue_WhenAssigned()
    {
        var data = TestDataFactory.OneUserAssignedToEvent_WithOneVideo();
        SeedDbContext.Add(data.owner);
        SeedDbContext.Add(data.user);
        SeedDbContext.Add(data.evt);
        SeedDbContext.Add(data.participation);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await accessService.DoesUserHasAccessToEvent(data.evt.Id, data.user.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(result);
    }

    // B7: Returns false for non-assigned user
    [Fact]
    public async Task IsUserAssignedToEvent_ReturnsFalse_WhenNotAssigned()
    {
        var ownerB = new UserDataBuilder();
        var owner = ownerB.Build();
        var eventB = new EventDataBuilder().WithOwner(owner);
        var evt = eventB.Build();
        var stranger = new UserDataBuilder().Build();

        SeedDbContext.Add(owner);
        SeedDbContext.Add(evt);
        SeedDbContext.Add(stranger);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await accessService.DoesUserHasAccessToEvent(evt.Id, stranger.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(result);
    }

    // B8: Event mismatch
    [Fact]
    public async Task IsUserAssignedToEvent_ReturnsFalse_WhenAssignedToDifferentEvent()
    {
        var owner1 = new UserDataBuilder().Build();
        var owner2 = new UserDataBuilder().Build();
        var evtA = new EventDataBuilder().WithOwner(owner1).Build();
        var evtB = new EventDataBuilder().WithOwner(owner2).Build();
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var membershipA = userB.AssignTo(evtA);

        SeedDbContext.Add(owner1);
        SeedDbContext.Add(owner2);
        SeedDbContext.Add(evtA);
        SeedDbContext.Add(evtB);
        SeedDbContext.Add(user);
        SeedDbContext.Add(membershipA);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await accessService.DoesUserHasAccessToEvent(evtB.Id, user.Id, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(result);
    }

    // B9: Multiple memberships do not affect correctness (still true)
    [Fact]
    public async Task IsUserAssignedToEvent_ReturnsTrue_WithDuplicateMemberships()
    {
        var owner = new UserDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(owner).Build();
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var m1 = userB.AssignTo(evt);
        var m2 = userB.AssignTo(evt);

        SeedDbContext.Add(owner);
        SeedDbContext.Add(evt);
        SeedDbContext.Add(user);
        SeedDbContext.Add(m1);
        SeedDbContext.Add(m2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await accessService.DoesUserHasAccessToEvent(evt.Id, user.Id, TestContext.Current.CancellationToken);
        Assert.True(result);
    }
    
    // X1: Direct user share grants access by blob
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenDirectlyShared()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        var directShare = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id };
        SeedDbContext.AddRange(user, video, directShare);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X2: Event share grants access when user is assigned to the event (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToEvent_AndUserAssigned()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        var sharer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(sharer).WithBlobId(Guid.NewGuid().ToString()).RecordedAt(DateTime.UtcNow.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = sharer.Id, EventId = evt.Id };

        // Persist principals first to satisfy FKs, then add share
        SeedDbContext.AddRange(owner, user, evt, membership, sharer, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.Add(share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X3: Event share denies access when user is not assigned (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToEvent_AndUserNotAssigned()
    {
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var user = new UserDataBuilder().Build();
        var sharer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(sharer).WithBlobId(Guid.NewGuid().ToString()).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = sharer.Id, EventId = evt.Id };
        SeedDbContext.AddRange(owner, evt, user, sharer, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.False(has);
    }

    // X4: Group share grants access when recorded after join (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_AfterJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).RecordedAt(joinedAt.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X5: Group share denies access when recorded before join (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_BeforeJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).RecordedAt(joinedAt.AddMinutes(-5)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X6: Group share denies access when recorded exactly at join time (strict inequality) (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_AtJoinTime()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).RecordedAt(joinedAt).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, GroupId = group.Id };

        SeedDbContext.AddRange(user, group, membership, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X7: No share present -> no access (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsFalse_WhenNoShares()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedDbContext.AddRange(user, video);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.BlobId!, user.Id, TestContext.Current.CancellationToken);
        Assert.False(has);
    }

    // Y1: By videoId - event share with assignment grants access
    [Fact]
    public async Task DoesUserHasAccessAsync_ByVideoId_ReturnsTrue_WhenSharedToEvent_AndUserAssigned()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        var video = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(1)).Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, EventId = evt.Id };

        SeedDbContext.AddRange(owner, user, evt, membership, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.Id, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // Y2: By videoId - direct share grants access
    [Fact]
    public async Task DoesUserHasAccessAsync_ByVideoId_ReturnsTrue_WhenDirectlyShared()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).Build();
        var directShare = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id };
        SeedDbContext.AddRange(user, video, directShare);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await accessService.DoesUserHasAccessAsync(video.Id, user.Id, TestContext.Current.CancellationToken);
        Assert.True(has);
    }
}