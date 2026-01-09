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

    // P1: GetUserPrivateVideos returns only private videos for the user
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsOnlyPrivateVideos_ForUser()
    {
        // Arrange
        var userB = new UserDataBuilder();
        var user = userB.Build();

        // Create 2 private videos for this user
        var privateVideo1 = new VideoDataBuilder().UploadedBy(user).WithName("Private1").Build();
        var privateShare1 = new SharedWith { Id = Guid.NewGuid(), VideoId = privateVideo1.Id, UserId = user.Id, EventId = null, GroupId = null };

        var privateVideo2 = new VideoDataBuilder().UploadedBy(user).WithName("Private2").Build();
        var privateShare2 = new SharedWith { Id = Guid.NewGuid(), VideoId = privateVideo2.Id, UserId = user.Id, EventId = null, GroupId = null };

        // Create a group video for this user (should not be returned)
        var group = new GroupDataBuilder().Build();
        var groupVideo = new VideoDataBuilder().UploadedBy(user).WithName("GroupVideo").Build();
        var groupShare = new SharedWith { Id = Guid.NewGuid(), VideoId = groupVideo.Id, UserId = user.Id, EventId = null, GroupId = group.Id };

        // Create an event video for this user (should not be returned)
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var eventVideo = new VideoDataBuilder().UploadedBy(user).WithName("EventVideo").Build();
        var eventShare = new SharedWith { Id = Guid.NewGuid(), VideoId = eventVideo.Id, UserId = user.Id, EventId = evt.Id, GroupId = null };

        SeedDbContext.AddRange(user, privateVideo1, privateShare1, privateVideo2, privateShare2,
                                group, groupVideo, groupShare, owner, evt, eventVideo, eventShare);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == privateVideo1.Id);
        Assert.Contains(result, v => v.Id == privateVideo2.Id);
        Assert.DoesNotContain(result, v => v.Id == groupVideo.Id);
        Assert.DoesNotContain(result, v => v.Id == eventVideo.Id);
    }

    // P2: GetUserPrivateVideos returns empty when user has no private videos
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsEmpty_WhenUserHasNoPrivateVideos()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    // P3: GetUserPrivateVideos does not return other users' private videos
    [Fact]
    public async Task GetUserPrivateVideos_DoesNotReturnOtherUsersPrivateVideos()
    {
        // Arrange
        var user1 = new UserDataBuilder().Build();
        var user2 = new UserDataBuilder().Build();

        var user1Video = new VideoDataBuilder().UploadedBy(user1).WithName("User1Private").Build();
        var user1Share = new SharedWith { Id = Guid.NewGuid(), VideoId = user1Video.Id, UserId = user1.Id, EventId = null, GroupId = null };

        var user2Video = new VideoDataBuilder().UploadedBy(user2).WithName("User2Private").Build();
        var user2Share = new SharedWith { Id = Guid.NewGuid(), VideoId = user2Video.Id, UserId = user2.Id, EventId = null, GroupId = null };

        SeedDbContext.AddRange(user1, user2, user1Video, user1Share, user2Video, user2Share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user1.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(user1Video.Id, result.First().Id);
        Assert.DoesNotContain(result, v => v.Id == user2Video.Id);
    }

    // P4: GetUserPrivateVideos returns videos with correct blob sizes
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsVideosWithBlobSizes()
    {
        // Arrange
        var user = new UserDataBuilder().Build();
        var video = new VideoDataBuilder()
            .UploadedBy(user)
            .WithName("PrivateWithSizes")
            .WithSourceBlobSize(1024)
            .WithConvertedBlobSize(2048)
            .Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, EventId = null, GroupId = null };

        SeedDbContext.AddRange(user, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        var returnedVideo = result.First();
        Assert.Equal(1024, returnedVideo.SourceBlobSize);
        Assert.Equal(2048, returnedVideo.ConvertedBlobSize);
    }

    // P5: GetUserPrivateVideos returns videos regardless of conversion status
    [Fact]
    public async Task GetUserPrivateVideos_ReturnsVideos_RegardlessOfConversionStatus()
    {
        // Arrange
        var user = new UserDataBuilder().Build();

        var convertedVideo = new VideoDataBuilder().UploadedBy(user).WithName("Converted").Converted(true).Build();
        var convertedShare = new SharedWith { Id = Guid.NewGuid(), VideoId = convertedVideo.Id, UserId = user.Id, EventId = null, GroupId = null };

        var unconvertedVideo = new VideoDataBuilder().UploadedBy(user).WithName("NotConverted").Converted(false).Build();
        var unconvertedShare = new SharedWith { Id = Guid.NewGuid(), VideoId = unconvertedVideo.Id, UserId = user.Id, EventId = null, GroupId = null };

        SeedDbContext.AddRange(user, convertedVideo, convertedShare, unconvertedVideo, unconvertedShare);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == convertedVideo.Id);
        Assert.Contains(result, v => v.Id == unconvertedVideo.Id);
    }

    // P6: GetUserPrivateVideos works correctly when user has mix of private, group, and event videos
    [Fact]
    public async Task GetUserPrivateVideos_FiltersCorrectly_WithMixedVideoTypes()
    {
        // Arrange
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();

        // 3 private videos
        var private1 = new VideoDataBuilder().UploadedBy(user).Build();
        var private2 = new VideoDataBuilder().UploadedBy(user).Build();
        var private3 = new VideoDataBuilder().UploadedBy(user).Build();
        var privateShare1 = new SharedWith { Id = Guid.NewGuid(), VideoId = private1.Id, UserId = user.Id, EventId = null, GroupId = null };
        var privateShare2 = new SharedWith { Id = Guid.NewGuid(), VideoId = private2.Id, UserId = user.Id, EventId = null, GroupId = null };
        var privateShare3 = new SharedWith { Id = Guid.NewGuid(), VideoId = private3.Id, UserId = user.Id, EventId = null, GroupId = null };

        // 2 group videos
        var group1 = new VideoDataBuilder().UploadedBy(user).Build();
        var group2 = new VideoDataBuilder().UploadedBy(user).Build();
        var groupShare1 = new SharedWith { Id = Guid.NewGuid(), VideoId = group1.Id, UserId = user.Id, EventId = null, GroupId = group.Id };
        var groupShare2 = new SharedWith { Id = Guid.NewGuid(), VideoId = group2.Id, UserId = user.Id, EventId = null, GroupId = group.Id };

        // 1 event video
        var event1 = new VideoDataBuilder().UploadedBy(user).Build();
        var eventShare1 = new SharedWith { Id = Guid.NewGuid(), VideoId = event1.Id, UserId = user.Id, EventId = evt.Id, GroupId = null };

        SeedDbContext.AddRange(user, owner, evt, group,
                                private1, private2, private3, privateShare1, privateShare2, privateShare3,
                                group1, group2, groupShare1, groupShare2,
                                event1, eventShare1);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await accessService.GetUserPrivateVideos(user.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, v => v.Id == private1.Id);
        Assert.Contains(result, v => v.Id == private2.Id);
        Assert.Contains(result, v => v.Id == private3.Id);
    }
    
    // P7 When user has private video, method should tell that user has access to it.
    [Fact]
    public async Task DoesUserHasAccessAsync_ReturnsTrueForPrivateVideo()
    {
        // Arrange
        var userB = new UserDataBuilder();
        var user = userB.Build();

        // Create private video for this user
        var privateVideo1 = new VideoDataBuilder().UploadedBy(user).WithName("Private1").Build();
        var privateShare1 = new SharedWith { Id = Guid.NewGuid(), VideoId = privateVideo1.Id, UserId = user.Id, EventId = null, GroupId = null };
        
        SeedDbContext.AddRange(user, privateVideo1, privateShare1);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await accessService.DoesUserHasAccessAsync(privateVideo1.BlobId!, user.Id, TestContext.Current.CancellationToken);
        
        Assert.True(results);
    }
}