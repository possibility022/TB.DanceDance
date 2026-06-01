using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Tests.Features.AccessManagement;

/// <summary>
/// Access decisions, post-split. Upload checks and event/group membership live in the Access module
/// (<see cref="CanUserUploadToEventRequest"/>, <see cref="DoesUserHasAccessToSharedWith"/>); per-video
/// access and private-video listing live in the Videos module
/// (<see cref="DoesUserHaveAccessToVideoByBlobQuery"/>, <see cref="ViewPrivateVideosQuery"/>) and reach
/// back into Access through the mediator. Everything is exercised through the real wiring.
/// </summary>
public class AccessServiceTests : BaseTestClass
{
    public AccessServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
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

        SeedAccessContext.AddRange(owner, user, evt, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await Send(new CanUserUploadToEventRequest { UserId = user.Id, EventId = evt.Id },
            TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(owner, evt, stranger);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await Send(new CanUserUploadToEventRequest { UserId = stranger.Id, EventId = evt.Id },
            TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await Send(new CanUserUploadToGroupRequest { UserId = user.Id, GroupId = group.Id },
            TestContext.Current.CancellationToken);
        Assert.True(can);
    }

    // G2: Cannot upload to group when not member
    [Fact]
    public async Task CanUserUploadToGroupAsync_ReturnsFalse_WhenNotAssigned()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedAccessContext.AddRange(user, group);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var can = await Send(new CanUserUploadToGroupRequest { UserId = user.Id, GroupId = group.Id },
            TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(user, groupA, groupB, memA, memB,
            ownerA, ownerB, evtA, evtB, partA, partB);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserGroupsAndEvents { UserId = user.Id }, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Groups.Count);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Groups, g => g.Id == groupA.Id);
        Assert.Contains(result.Groups, g => g.Id == groupB.Id);
        Assert.Contains(result.Events, e => e.Id == evtA.Id);
        Assert.Contains(result.Events, e => e.Id == evtB.Id);
    }

    // B: Returns empty when user has no memberships
    [Fact]
    public async Task GetUserEventsAndGroupsAsync_ReturnsEmpty_WhenNoMemberships()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserGroupsAndEvents { UserId = user.Id }, TestContext.Current.CancellationToken);
        Assert.Empty(result.Groups);
        Assert.Empty(result.Events);
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

        SeedAccessContext.AddRange(user, group, owner, evt, m1, m2, p1, p2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new GetUserGroupsAndEvents { UserId = user.Id }, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Groups.Count); // duplicate groups expected
        Assert.Equal(2, result.Events.Count); // duplicate events expected
        Assert.True(result.Groups.All(g => g.Id == group.Id));
        Assert.True(result.Events.All(e => e.Id == evt.Id));
    }

     // B6: User assigned to event has access
    [Fact]
    public async Task IsUserAssignedToEvent_ReturnsTrue_WhenAssigned()
    {
        var data = TestDataFactory.OneUserAssignedToEvent_WithOneVideo();
        SeedAccessContext.AddRange(data.owner, data.user, data.evt, data.participation);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DoesUserHasAccessToSharedWith
        {
            UserId = data.user.Id,
            SharedToId = data.evt.Id,
            SharedWithType = SharedWithByType.Event,
        }, TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(owner, evt, stranger);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DoesUserHasAccessToSharedWith
        {
            UserId = stranger.Id,
            SharedToId = evt.Id,
            SharedWithType = SharedWithByType.Event,
        }, TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(owner1, owner2, evtA, evtB, user, membershipA);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DoesUserHasAccessToSharedWith
        {
            UserId = user.Id,
            SharedToId = evtB.Id,
            SharedWithType = SharedWithByType.Event,
        }, TestContext.Current.CancellationToken);
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

        SeedAccessContext.AddRange(owner, evt, user, m1, m2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DoesUserHasAccessToSharedWith
        {
            UserId = user.Id,
            SharedToId = evt.Id,
            SharedWithType = SharedWithByType.Event,
        }, TestContext.Current.CancellationToken);
        Assert.True(result);
    }

    // X1: Direct user share grants access by blob
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenDirectlyShared()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString())
            .ShareWithUser(user).Build();
        SeedAccessContext.Add(user);
        SeedVideosContext.Add(video);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
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
        var video = new VideoDataBuilder().UploadedBy(sharer).WithBlobId(Guid.NewGuid().ToString())
            .RecordedAt(DateTime.UtcNow.AddMinutes(1)).ShareWithEvent(evt, sharer).Build();

        SeedAccessContext.AddRange(owner, user, evt, membership, sharer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X3: Event share denies access when user is not assigned (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsFalse_WhenSharedToEvent_AndUserNotAssigned()
    {
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var user = new UserDataBuilder().Build();
        var sharer = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(sharer).WithBlobId(Guid.NewGuid().ToString())
            .ShareWithEvent(evt, sharer).Build();

        SeedAccessContext.AddRange(owner, evt, user, sharer);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
        Assert.False(has);
    }

    // X4: Group share grants access regardless of join time (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_AfterJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString())
            .RecordedAt(joinedAt.AddMinutes(1)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X5: Group share grants access even when recorded before join (no time restriction on per-video access)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_BeforeJoin()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString())
            .RecordedAt(joinedAt.AddMinutes(-5)).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X6: Group share grants access at join time (no time restriction on per-video access)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsTrue_WhenSharedToGroup_AtJoinTime()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joinedAt = DateTime.UtcNow;
        var membership = userB.AssignTo(group, joinedAt);

        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString())
            .RecordedAt(joinedAt).ShareWithGroup(group, user).Build();

        SeedAccessContext.AddRange(user, group, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // X7: No share present -> no access (blob overload)
    [Fact]
    public async Task DoesUserHasAccessAsync_ByBlob_ReturnsFalse_WhenNoShares()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).WithBlobId(Guid.NewGuid().ToString()).Build();
        SeedAccessContext.Add(user);
        SeedVideosContext.Add(video);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, video.BlobId!),
            TestContext.Current.CancellationToken);
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

        var video = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(1))
            .ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(owner, user, evt, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoQuery(user.Id, video.Id),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // Y2: By videoId - direct share grants access
    [Fact]
    public async Task DoesUserHasAccessAsync_ByVideoId_ReturnsTrue_WhenDirectlyShared()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder().UploadedBy(user).ShareWithUser(user).Build();
        SeedAccessContext.Add(user);
        SeedVideosContext.Add(video);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var has = await Send(new DoesUserHaveAccessToVideoQuery(user.Id, video.Id),
            TestContext.Current.CancellationToken);
        Assert.True(has);
    }

    // P1: Private videos query returns only private videos for the user
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsOnlyPrivateVideos_ForUser()
    {
        var user = new UserDataBuilder().Build();

        var privateVideo1 = new VideoDataBuilder().UploadedBy(user).WithName("Private1").ShareAsPrivate(user).Build();
        var privateVideo2 = new VideoDataBuilder().UploadedBy(user).WithName("Private2").ShareAsPrivate(user).Build();

        var group = new GroupDataBuilder().Build();
        var groupVideo = new VideoDataBuilder().UploadedBy(user).WithName("GroupVideo").ShareWithGroup(group, user).Build();

        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var eventVideo = new VideoDataBuilder().UploadedBy(user).WithName("EventVideo").ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(user, group, owner, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(privateVideo1, privateVideo2, groupVideo, eventVideo);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == privateVideo1.Id);
        Assert.Contains(result, v => v.Id == privateVideo2.Id);
        Assert.DoesNotContain(result, v => v.Id == groupVideo.Id);
        Assert.DoesNotContain(result, v => v.Id == eventVideo.Id);
    }

    // P2: Private videos query returns empty when user has no private videos
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsEmpty_WhenUserHasNoPrivateVideos()
    {
        var user = new UserDataBuilder().Build();
        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user.Id), TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    // P3: Private videos query does not return other users' private videos
    [Fact]
    public async Task GetUserPrivateVideos_DoesNotReturnOtherUsersPrivateVideos()
    {
        var user1 = new UserDataBuilder().Build();
        var user2 = new UserDataBuilder().Build();

        var user1Video = new VideoDataBuilder().UploadedBy(user1).WithName("User1Private").ShareAsPrivate(user1).Build();
        var user2Video = new VideoDataBuilder().UploadedBy(user2).WithName("User2Private").ShareAsPrivate(user2).Build();

        SeedAccessContext.AddRange(user1, user2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(user1Video, user2Video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user1.Id), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(user1Video.Id, result.First().Id);
        Assert.DoesNotContain(result, v => v.Id == user2Video.Id);
    }

    // P4: Private videos are returned; sizes are stored on the entity (VideoDto does not carry sizes)
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsVideosWithBlobSizes()
    {
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(user)
            .WithName("PrivateWithSizes")
            .WithSourceBlobSize(1024)
            .WithConvertedBlobSize(2048)
            .ShareAsPrivate(user)
            .Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(video.Id, result.First().Id);
        // VideoDto omits sizes; verify they were persisted.
        var saved = await SeedVideosContext.Videos.AsNoTracking().FirstAsync(v => v.Id == video.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1024, saved.SourceBlobSize);
        Assert.Equal(2048, saved.ConvertedBlobSize);
    }

    // P5: Private videos query returns videos regardless of conversion status
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsVideos_RegardlessOfConversionStatus()
    {
        var user = new UserDataBuilder().Build();

        var convertedVideo = new VideoDataBuilder().UploadedBy(user).WithName("Converted").Converted(true).ShareAsPrivate(user).Build();
        var unconvertedVideo = new VideoDataBuilder().UploadedBy(user).WithName("NotConverted").Converted(false).ShareAsPrivate(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(convertedVideo, unconvertedVideo);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == convertedVideo.Id);
        Assert.Contains(result, v => v.Id == unconvertedVideo.Id);
    }

    // P6: Private videos query filters correctly with a mix of private, group, and event videos
    [Fact]
    public async Task GetUserPrivateVideos_FiltersCorrectly_WithMixedVideoTypes()
    {
        var user = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();

        var private1 = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var private2 = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();
        var private3 = new VideoDataBuilder().UploadedBy(user).ShareAsPrivate(user).Build();

        var group1 = new VideoDataBuilder().UploadedBy(user).ShareWithGroup(group, user).Build();
        var group2 = new VideoDataBuilder().UploadedBy(user).ShareWithGroup(group, user).Build();

        var event1 = new VideoDataBuilder().UploadedBy(user).ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(user, owner, evt, group);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(private1, private2, private3, group1, group2, event1);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewPrivateVideosQuery(user.Id), TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, v => v.Id == private1.Id);
        Assert.Contains(result, v => v.Id == private2.Id);
        Assert.Contains(result, v => v.Id == private3.Id);
    }

    // P7: When user has a private video, they have access to it
    [Fact]
    public async Task DoesUserHasAccessAsync_ReturnsTrueForPrivateVideo()
    {
        var user = new UserDataBuilder().Build();
        var privateVideo1 = new VideoDataBuilder().UploadedBy(user).WithName("Private1")
            .WithBlobId(Guid.NewGuid().ToString()).ShareAsPrivate(user).Build();

        SeedAccessContext.Add(user);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(privateVideo1);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new DoesUserHaveAccessToVideoByBlobQuery(user.Id, privateVideo1.BlobId!),
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }
}
