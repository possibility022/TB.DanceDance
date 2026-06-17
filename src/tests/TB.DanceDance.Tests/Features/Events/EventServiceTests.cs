using Application.Features.Events;
using Domain.Entities;
using Infrastructure.Data;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Events;

public class EventServiceTests : BaseTestClass
{
    private EventService eventService = null!;

    public EventServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        this.eventService = new EventService(runtimeDbContext);
        return ValueTask.CompletedTask;
    }

    // C10: Returns videos shared with the event for an assigned user
    [Fact]
    public async Task GetVideos_ReturnsOnlyEventVideos_ForAssignedUser()
    {
        var testData = TestDataFactory.OneUserAssignedToEvent_WithOneVideo();
        SeedDbContext.Add(testData.owner);
        SeedDbContext.Add(testData.user);
        SeedDbContext.Add(testData.evt);
        SeedDbContext.Add(testData.video);
        SeedDbContext.Add(testData.eventShare);
        SeedDbContext.Add(testData.participation);

        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (videos, _) =
            await eventService.GetVideos(testData.evt.Id, testData.user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);

        Assert.Single(videos);
        Assert.Equal(testData.video.Id, videos.Single().Id);
    }

    // A1: CreateEventAsync sets Id and persists event
    [Fact]
    public async Task CreateEventAsync_SetsId_And_PersistsEvent()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        // Persist owner for FK
        SeedDbContext.Add(owner);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var created = await eventService.CreateEventAsync(evt, CancellationToken.None);
        SeedDbContext.ChangeTracker.Clear();

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(SeedDbContext.Events.Any(e => e.Id == created.Id));
    }

    // A2/A3: Owner auto-membership added once
    [Fact]
    public async Task CreateEventAsync_AddsOwnerMembershipOnce()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedDbContext.Add(owner);
        await SeedDbContext.SaveChangesAsync();

        var created = await eventService.CreateEventAsync(evt, TestContext.Current.CancellationToken);

        SeedDbContext.ChangeTracker.Clear();
        var ownerMemberships = SeedDbContext.AssingedToEvents
            .Where(a => a.EventId == created.Id && a.UserId == created.Owner).ToList();
        Assert.Single(ownerMemberships);
    }

    // A4: Owner provided explicitly is respected
    [Fact]
    public async Task CreateEventAsync_RespectsExplicitOwner()
    {
        var ownerB = new UserDataBuilder();
        var owner = ownerB.Build();
        var eventB = new EventDataBuilder().WithOwner(owner);
        var evt = eventB.Build();

        SeedDbContext.Add(owner);
        await SeedDbContext.SaveChangesAsync();

        var created = await eventService.CreateEventAsync(evt, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(owner.Id, created.Owner);
        Assert.True(SeedDbContext.AssingedToEvents.Any(a => a.EventId == created.Id && a.UserId == owner.Id));
    }

    // C11: Filters out videos shared to other events
    [Fact]
    public async Task GetVideos_ExcludesOtherEvents()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var evtAB = new EventDataBuilder();
        var ownerA = evtAB.BuildOwner();
        var evtA = evtAB.Build();
        var evtBB = new EventDataBuilder();
        var ownerB = evtBB.BuildOwner();
        var evtB = evtBB.Build();

        var membershipA = userB.AssignTo(evtA);

        var videoForB = new VideoDataBuilder().OwnedBy(user).Build();
        var shareToB = new VideoDataBuilder().WithId(videoForB.Id); // placeholder

        SeedDbContext.Add(ownerA);
        SeedDbContext.Add(ownerB);
        SeedDbContext.Add(user);
        SeedDbContext.AddRange(evtA, evtB);
        SeedDbContext.Add(membershipA);
        SeedDbContext.Add(videoForB);
        // Share video with B
        SeedDbContext.Add(new SharedWith
        {
            Id = Guid.NewGuid(), VideoId = videoForB.Id, UserId = user.Id, EventId = evtB.Id
        });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (videos, _) = await eventService.GetVideos(evtA.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Empty(videos);
    }

    // C12: Filters out videos shared to groups or directly to user
    [Fact]
    public async Task GetVideos_ExcludesGroupAndDirectShares()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        var group = new GroupDataBuilder().Build();

        var videoToGroup = new VideoDataBuilder().OwnedBy(user).Build();
        var shareGroup = new SharedWith
        {
            Id = Guid.NewGuid(), VideoId = videoToGroup.Id, UserId = user.Id, GroupId = group.Id
        };

        var videoDirect = new VideoDataBuilder().OwnedBy(user).Build();
        var shareDirect = new SharedWith { Id = Guid.NewGuid(), VideoId = videoDirect.Id, UserId = user.Id };

        SeedDbContext.Add(owner);
        SeedDbContext.Add(user);
        SeedDbContext.Add(evt);
        SeedDbContext.Add(membership);
        SeedDbContext.Add(group);
        SeedDbContext.AddRange(videoToGroup, videoDirect);
        SeedDbContext.AddRange(shareGroup, shareDirect);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (videos, _) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Empty(videos);
    }

    // C13: User not assigned -> no videos even if event has shares
    [Fact]
    public async Task GetVideos_ReturnsEmpty_WhenUserNotAssigned()
    {
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var user = new UserDataBuilder().Build();

        var vidB = new VideoDataBuilder().OwnedBy(user);
        var video = vidB.Build();
        var share = new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, EventId = evt.Id };

        SeedDbContext.AddRange(owner, user, evt, video, share);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (videos, _) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Empty(videos);
    }

    // C14/C15: Multiple videos returned sorted by RecordedDateTime desc
    [Fact]
    public async Task GetVideos_SortsByRecordedDateTime_Descending()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        var v1 = new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-3)).Build();
        var v2 = new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-1)).Build();
        var v3 = new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-2)).Build();

        SeedDbContext.AddRange(owner, user, evt, membership, v1, v2, v3);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = v1.Id, UserId = user.Id, EventId = evt.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = v2.Id, UserId = user.Id, EventId = evt.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = v3.Id, UserId = user.Id, EventId = evt.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (result, _) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { v2.Id, v3.Id, v1.Id }, result.Select(r => r.Id).ToArray());
    }

    // C16: Duplicate shares produce duplicate videos in result (document current behavior)
    [Fact]
    public async Task GetVideos_ReturnsDuplicates_WhenDuplicateSharesExist()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);
        var video = new VideoDataBuilder().OwnedBy(user).Build();

        SeedDbContext.AddRange(owner, user, evt, membership, video);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, EventId = evt.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = user.Id, EventId = evt.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (result, _) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal(video.Id, v.Id));
    }

    // C17: User assigned to multiple events; query isolates by eventId
    [Fact]
    public async Task GetVideos_IsolatedByEventId_WhenUserAssignedToMultipleEvents()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();

        var evtAB = new EventDataBuilder();
        var ownerA = evtAB.BuildOwner();
        var evtA = evtAB.Build();
        var evtBB = new EventDataBuilder();
        var ownerB = evtBB.BuildOwner();
        var evtB = evtBB.Build();

        var memA = userB.AssignTo(evtA);
        var memB = userB.AssignTo(evtB);

        var vA = new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(-5)).Build();
        var vB = new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(-4)).Build();

        SeedDbContext.AddRange(ownerA, ownerB, user, evtA, evtB, memA, memB, vA, vB);
        SeedDbContext.AddRange(
            new SharedWith { Id = Guid.NewGuid(), VideoId = vA.Id, UserId = user.Id, EventId = evtA.Id },
            new SharedWith { Id = Guid.NewGuid(), VideoId = vB.Id, UserId = user.Id, EventId = evtB.Id }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (resultA, _) = await eventService.GetVideos(evtA.Id, user.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Single(resultA);
        Assert.Equal(vA.Id, resultA.Single().Id);
    }

    // C18: Owner-only case when event created via service
    [Fact]
    public async Task GetVideos_OwnerSeesVideos_WhenCreatedViaService()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedDbContext.Add(owner);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var created = await eventService.CreateEventAsync(evt, TestContext.Current.CancellationToken);

        var video = new VideoDataBuilder().OwnedBy(owner).Build();
        SeedDbContext.AddRange(video,
            new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = owner.Id, EventId = created.Id });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (result, _) = await eventService.GetVideos(created.Id, owner.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(video.Id, result.Single().Id);
    }

    // C19: Event created outside service without owner membership -> owner cannot see videos
    [Fact]
    public async Task GetVideos_ReturnsEmpty_WhenEventCreatedOutsideService_WithoutOwnerMembership()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedDbContext.AddRange(owner, evt);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var video = new VideoDataBuilder().OwnedBy(owner).Build();
        SeedDbContext.AddRange(video,
            new SharedWith { Id = Guid.NewGuid(), VideoId = video.Id, UserId = owner.Id, EventId = evt.Id });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (result, _) = await eventService.GetVideos(evt.Id, owner.Id, pageNumber: 1, pageSize: 20, TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Empty(result);
    }

    // C20: GetVideos first page returns PageSize items in RecordedDateTime-descending order plus correct TotalCount
    [Fact]
    public async Task GetVideos_FirstPage_ReturnsPageSizeItemsAndTotalCount()
    {
        var (user, evt, videos) = await SeedFiveEventVideos();

        var (firstPage, totalCount) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 1, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(5, totalCount);
        var list = firstPage.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(videos[4].Id, list[0].Id);
        Assert.Equal(videos[3].Id, list[1].Id);
    }

    // C21: GetVideos second page returns the remainder in the same order
    [Fact]
    public async Task GetVideos_SecondPage_ReturnsRemainderInOrder()
    {
        var (user, evt, videos) = await SeedFiveEventVideos();

        var (secondPage, totalCount) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 2, pageSize: 2, TestContext.Current.CancellationToken);

        Assert.Equal(5, totalCount);
        var list = secondPage.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(videos[2].Id, list[0].Id);
        Assert.Equal(videos[1].Id, list[1].Id);
    }

    // C22: GetVideos out-of-range page returns no items but the correct TotalCount
    [Fact]
    public async Task GetVideos_OutOfRangePage_ReturnsEmptyItemsWithCorrectTotalCount()
    {
        var (user, evt, _) = await SeedFiveEventVideos();

        var (items, totalCount) = await eventService.GetVideos(evt.Id, user.Id, pageNumber: 999, pageSize: 10, TestContext.Current.CancellationToken);

        Assert.Equal(5, totalCount);
        Assert.Empty(items);
    }

    private async Task<(User user, Event evt, Video[] videos)> SeedFiveEventVideos()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var evtB = new EventDataBuilder();
        var owner = evtB.BuildOwner();
        var evt = evtB.Build();
        var membership = userB.AssignTo(evt);

        var videos = Enumerable.Range(0, 5)
            .Select(i => new VideoDataBuilder().OwnedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(-10 + i)).Build())
            .ToArray();
        var shares = videos
            .Select(v => new SharedWith { Id = Guid.NewGuid(), VideoId = v.Id, UserId = user.Id, EventId = evt.Id })
            .ToArray();

        SeedDbContext.AddRange(owner, user, evt, membership);
        SeedDbContext.AddRange(videos);
        SeedDbContext.AddRange(shares);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedDbContext.ChangeTracker.Clear();

        return (user, evt, videos);
    }
}