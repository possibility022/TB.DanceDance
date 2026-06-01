using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Features.Events;
using TB.DanceDance.Tests.TestsFixture;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Tests.Features.Events;

/// <summary>
/// Event video listing (<see cref="ViewVideosFromEventQuery"/>, Videos module — gated by Access event
/// membership through the mediator) and event creation (<see cref="CreateEventCommand"/>, Access module,
/// which auto-adds the owner's membership).
/// </summary>
public class EventServiceTests : BaseTestClass
{
    public EventServiceTests(DanceDbFixture danceDbFixture) : base(danceDbFixture)
    {
    }

    // C10: Returns videos shared with the event for an assigned user
    [Fact]
    public async Task GetVideos_ReturnsOnlyEventVideos_ForAssignedUser()
    {
        var testData = TestDataFactory.OneUserAssignedToEvent_WithOneVideo();
        SeedAccessContext.AddRange(testData.owner, testData.user, testData.evt, testData.participation);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(testData.video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var videos = await Send(new ViewVideosFromEventQuery(testData.user.Id, testData.evt.Id),
            TestContext.Current.CancellationToken);

        Assert.Single(videos);
        Assert.Equal(testData.video.Id, videos.Single().Id);
    }

    // A1: CreateEventCommand returns a new id and persists the event
    [Fact]
    public async Task CreateEventAsync_SetsId_And_PersistsEvent()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdId = await Send(new CreateEventCommand(evt.Name, evt.Date, owner.Id), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, createdId);
        Assert.True(await SeedAccessContext.Events.AnyAsync(e => e.Id == createdId, TestContext.Current.CancellationToken));
    }

    // A2/A3: Owner auto-membership added once
    [Fact]
    public async Task CreateEventAsync_AddsOwnerMembershipOnce()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdId = await Send(new CreateEventCommand(evt.Name, evt.Date, owner.Id), TestContext.Current.CancellationToken);

        var ownerMemberships = await SeedAccessContext.AssignedToEvents
            .Where(a => a.EventId == createdId && a.UserId == owner.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
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

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdId = await Send(new CreateEventCommand(evt.Name, evt.Date, owner.Id), TestContext.Current.CancellationToken);

        var created = await SeedAccessContext.Events.AsNoTracking().FirstAsync(e => e.Id == createdId, TestContext.Current.CancellationToken);
        Assert.Equal(owner.Id, created.Owner);
        Assert.True(await SeedAccessContext.AssignedToEvents.AnyAsync(a => a.EventId == createdId && a.UserId == owner.Id, TestContext.Current.CancellationToken));
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

        var videoForB = new VideoDataBuilder().UploadedBy(user).ShareWithEvent(evtB, user).Build();

        SeedAccessContext.AddRange(ownerA, ownerB, user, evtA, evtB, membershipA);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(videoForB);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var videos = await Send(new ViewVideosFromEventQuery(user.Id, evtA.Id), TestContext.Current.CancellationToken);
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

        var videoToGroup = new VideoDataBuilder().UploadedBy(user).ShareWithGroup(group, user).Build();
        var videoDirect = new VideoDataBuilder().UploadedBy(user).ShareWithUser(user).Build();

        SeedAccessContext.AddRange(owner, user, evt, membership, group);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(videoToGroup, videoDirect);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var videos = await Send(new ViewVideosFromEventQuery(user.Id, evt.Id), TestContext.Current.CancellationToken);
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

        var video = new VideoDataBuilder().UploadedBy(user).ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(owner, user, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var videos = await Send(new ViewVideosFromEventQuery(user.Id, evt.Id), TestContext.Current.CancellationToken);
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

        var v1 = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-3)).ShareWithEvent(evt, user).Build();
        var v2 = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-1)).ShareWithEvent(evt, user).Build();
        var v3 = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddHours(-2)).ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(owner, user, evt, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(v1, v2, v3);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromEventQuery(user.Id, evt.Id), TestContext.Current.CancellationToken);
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
        var video = new VideoDataBuilder().UploadedBy(user).ShareWithEvent(evt, user).ShareWithEvent(evt, user).Build();

        SeedAccessContext.AddRange(owner, user, evt, membership);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromEventQuery(user.Id, evt.Id), TestContext.Current.CancellationToken);
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

        var vA = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(-5)).ShareWithEvent(evtA, user).Build();
        var vB = new VideoDataBuilder().UploadedBy(user).RecordedAt(DateTime.UtcNow.AddMinutes(-4)).ShareWithEvent(evtB, user).Build();

        SeedAccessContext.AddRange(ownerA, ownerB, user, evtA, evtB, memA, memB);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        SeedVideosContext.AddRange(vA, vB);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var resultA = await Send(new ViewVideosFromEventQuery(user.Id, evtA.Id), TestContext.Current.CancellationToken);
        Assert.Single(resultA);
        Assert.Equal(vA.Id, resultA.Single().Id);
    }

    // C18: Owner-only case when event created via the command (owner gets auto-membership)
    [Fact]
    public async Task GetVideos_OwnerSeesVideos_WhenCreatedViaService()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedAccessContext.Add(owner);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var createdId = await Send(new CreateEventCommand(evt.Name, evt.Date, owner.Id), TestContext.Current.CancellationToken);

        var video = new VideoDataBuilder().UploadedBy(owner).ShareWithEvent(createdId, owner.Id).Build();
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromEventQuery(owner.Id, createdId), TestContext.Current.CancellationToken);
        Assert.Single(result);
        Assert.Equal(video.Id, result.Single().Id);
    }

    // C19: Event created without owner membership -> owner cannot see videos
    [Fact]
    public async Task GetVideos_ReturnsEmpty_WhenEventCreatedOutsideService_WithoutOwnerMembership()
    {
        var eventB = new EventDataBuilder();
        var owner = eventB.BuildOwner();
        var evt = eventB.Build();

        SeedAccessContext.AddRange(owner, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var video = new VideoDataBuilder().UploadedBy(owner).ShareWithEvent(evt, owner).Build();
        SeedVideosContext.Add(video);
        await SeedVideosContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Send(new ViewVideosFromEventQuery(owner.Id, evt.Id), TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }
}
