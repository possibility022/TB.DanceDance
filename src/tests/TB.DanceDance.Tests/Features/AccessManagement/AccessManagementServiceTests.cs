using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.AccessManagement;

/// <summary>
/// Access-management write/read flows in the Access module: requesting access, approving/declining,
/// adding users, and listing pending/approvable requests — all driven through the mediator.
/// </summary>
public class AccessManagementServiceTests : BaseTestClass
{
    public AccessManagementServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    [Fact]
    public async Task SaveEventsAssigmentRequest_AddsRequests_AndSkipsPendingDuplicates()
    {
        var requestor = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var evt1 = new EventDataBuilder().WithOwner(owner).Build();
        var evt2 = new EventDataBuilder().WithOwner(owner).Build();

        SeedAccessContext.AddRange(owner, requestor, evt1, evt2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new SaveEventsAssignmentCommand { UserId = requestor.Id, Events = [evt1.Id, evt2.Id] }, TestContext.Current.CancellationToken);
        // Attempt to add duplicates again
        await Send(new SaveEventsAssignmentCommand { UserId = requestor.Id, Events = [evt1.Id, evt2.Id] }, TestContext.Current.CancellationToken);

        var all = await SeedAccessContext.EventAssignmentRequests.AsNoTracking().Where(r => r.UserId == requestor.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, all.Count);
        Assert.All(all, r => Assert.Null(r.Approved));
        Assert.Contains(all, r => r.EventId == evt1.Id);
        Assert.Contains(all, r => r.EventId == evt2.Id);
    }

    [Fact]
    public async Task SaveGroupsAssigmentRequests_AddsRequestsWithJoinDate_AndSkipsPendingDuplicates()
    {
        var requestor = new UserDataBuilder().Build();
        var group1 = new GroupDataBuilder().Build();
        var group2 = new GroupDataBuilder().Build();
        var joined1 = DateTime.UtcNow.AddDays(-3);
        var joined2 = DateTime.UtcNow.AddDays(-2);

        SeedAccessContext.AddRange(requestor, group1, group2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await Send(new SaveGroupsAssignmentCommand { UserId = requestor.Id, Groups = [(group1.Id, joined1), (group2.Id, joined2)] }, TestContext.Current.CancellationToken);
        // Attempt duplicates
        await Send(new SaveGroupsAssignmentCommand { UserId = requestor.Id, Groups = [(group1.Id, joined1), (group2.Id, joined2)] }, TestContext.Current.CancellationToken);

        var all = await SeedAccessContext.GroupAssignmentRequests.AsNoTracking().Where(r => r.UserId == requestor.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, all.Count);
        Assert.All(all, r => Assert.Null(r.Approved));
        var g1 = all.Single(r => r.GroupId == group1.Id);
        var g2 = all.Single(r => r.GroupId == group2.Id);
        Assert.True((g1.WhenJoined - joined1).Duration() < TimeSpan.FromMilliseconds(5));
        Assert.True((g2.WhenJoined - joined2).Duration() < TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public async Task AddOrUpdateUserAsync_AddsThenUpdatesUserNames()
    {
        var user = new UserDataBuilder().Build();

        await Send(new AddOrUpdateUserCommand { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email }, TestContext.Current.CancellationToken);
        var saved = await SeedAccessContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);

        await Send(new AddOrUpdateUserCommand { Id = user.Id, FirstName = "UpdatedFirst", LastName = "UpdatedLast", Email = user.Email + ".ignored" }, TestContext.Current.CancellationToken);

        SeedAccessContext.ChangeTracker.Clear();
        var updated = await SeedAccessContext.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal("UpdatedFirst", updated.FirstName);
        Assert.Equal("UpdatedLast", updated.LastName);
    }

    [Fact]
    public async Task GetPendingUserRequests_ReturnsPendingAndRejected_ExcludesApproved()
    {
        var user = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var evt1 = new EventDataBuilder().WithOwner(owner).Build();
        var evt2 = new EventDataBuilder().WithOwner(owner).Build();
        var group1 = new GroupDataBuilder().Build();
        var group2 = new GroupDataBuilder().Build();

        SeedAccessContext.AddRange(owner, user, evt1, evt2, group1, group2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var er1 = EventAssignmentRequest.Factory.Create(user.Id, evt1.Id); // pending
        var er2 = EventAssignmentRequest.Factory.Create(user.Id, evt2.Id);
        er2.Approved = true;
        var gr1 = GroupAssignmentRequest.Factory.Create(user.Id, group1.Id, DateTime.UtcNow);
        gr1.Approved = false; // rejected
        var gr2 = GroupAssignmentRequest.Factory.Create(user.Id, group2.Id, DateTime.UtcNow);
        gr2.Approved = true;

        SeedAccessContext.EventAssignmentRequests.AddRange(er1, er2);
        SeedAccessContext.GroupAssignmentRequests.AddRange(gr1, gr2);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pending = await Send(new GetPendingUserRequestsQuery { UserId = user.Id }, TestContext.Current.CancellationToken);
        Assert.Contains(evt1.Id, pending.Events);
        Assert.DoesNotContain(evt2.Id, pending.Events);
        Assert.Contains(group1.Id, pending.Groups);
        Assert.DoesNotContain(group2.Id, pending.Groups);
    }

    [Fact]
    public async Task GetAccessRequestsToApproveAsync_ReturnsEventAndGroupRequests_ForOwnerAndAdmin()
    {
        var approver = new UserDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(approver).Build();

        var groupAdmin = GroupAdmin.Factory.Create(approver.Id, group.Id);

        SeedAccessContext.AddRange(approver, requestor, group, groupAdmin, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var whenJoined = DateTime.UtcNow.AddDays(-1);
        SeedAccessContext.GroupAssignmentRequests.Add(GroupAssignmentRequest.Factory.Create(requestor.Id, group.Id, whenJoined));
        SeedAccessContext.EventAssignmentRequests.Add(EventAssignmentRequest.Factory.Create(requestor.Id, evt.Id));
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await Send(new GetAccessRequestsToApproveQuery { UserId = approver.Id }, TestContext.Current.CancellationToken);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.IsGroup && r.Name == group.Name);
        Assert.Contains(results, r => !r.IsGroup && r.Name == evt.Name);
    }

    [Fact]
    public async Task ApproveAccessRequest_Group_CreatesMembershipAndMarksApproved()
    {
        var approver = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var admin = GroupAdmin.Factory.Create(approver.Id, group.Id);

        SeedAccessContext.AddRange(approver, requestor, group, admin);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var whenJoined = DateTime.UtcNow.AddDays(-5);
        var request = GroupAssignmentRequest.Factory.Create(requestor.Id, group.Id, whenJoined);
        SeedAccessContext.GroupAssignmentRequests.Add(request);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new ApproveAccessRequestCommand { RequestId = request.Id, IsGroup = true, UserId = approver.Id }, TestContext.Current.CancellationToken);
        Assert.True(ok);

        SeedAccessContext.ChangeTracker.Clear();
        var membership = await SeedAccessContext.AssignedToGroups.AsNoTracking().SingleAsync(a => a.GroupId == group.Id && a.UserId == requestor.Id, TestContext.Current.CancellationToken);
        Assert.True((membership.WhenJoined - whenJoined).Duration() < TimeSpan.FromMilliseconds(5));

        var updatedRequest = await SeedAccessContext.GroupAssignmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest.Approved == true);
        Assert.Equal(approver.Id, updatedRequest.ManagedBy);
    }

    [Fact]
    public async Task ApproveAccessRequest_Group_ReturnsFalse_WhenUnauthorized()
    {
        var approver = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var admin = GroupAdmin.Factory.Create(approver.Id, group.Id);
        var requestor = new UserDataBuilder().Build();

        SeedAccessContext.AddRange(approver, unauthorized, requestor, group, admin);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = GroupAssignmentRequest.Factory.Create(requestor.Id, group.Id, DateTime.UtcNow);
        SeedAccessContext.GroupAssignmentRequests.Add(request);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new ApproveAccessRequestCommand { RequestId = request.Id, IsGroup = true, UserId = unauthorized.Id }, TestContext.Current.CancellationToken);
        Assert.False(ok);

        SeedAccessContext.ChangeTracker.Clear();
        Assert.False(await SeedAccessContext.AssignedToGroups.AnyAsync(a => a.GroupId == group.Id && a.UserId == requestor.Id, TestContext.Current.CancellationToken));
        var unchanged = await SeedAccessContext.GroupAssignmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.Null(unchanged.Approved);
        Assert.Null(unchanged.ManagedBy);
    }

    [Fact]
    public async Task ApproveAccessRequest_Event_CreatesMembershipAndMarksApproved()
    {
        var owner = new UserDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(owner).Build();

        SeedAccessContext.AddRange(owner, requestor, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = EventAssignmentRequest.Factory.Create(requestor.Id, evt.Id);
        SeedAccessContext.EventAssignmentRequests.Add(request);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Ensure this request is approvable by the owner (matches the query the API uses)
        var approvables = await Send(new GetAccessRequestsToApproveQuery { UserId = owner.Id }, TestContext.Current.CancellationToken);
        var approvableEventRequest = approvables.Single(r => !r.IsGroup && r.Name == evt.Name);

        var ok = await Send(new ApproveAccessRequestCommand { RequestId = approvableEventRequest.RequestId, IsGroup = false, UserId = owner.Id }, TestContext.Current.CancellationToken);
        Assert.True(ok);

        SeedAccessContext.ChangeTracker.Clear();
        Assert.True(await SeedAccessContext.AssignedToEvents.AnyAsync(a => a.EventId == evt.Id && a.UserId == requestor.Id, TestContext.Current.CancellationToken));
        var updatedRequest = await SeedAccessContext.EventAssignmentRequests.AsNoTracking().FirstAsync(r => r.EventId == evt.Id && r.UserId == requestor.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest.Approved == true);
        Assert.Equal(owner.Id, updatedRequest.ManagedBy);
    }

    [Fact]
    public async Task DeclineAccessRequest_Group_MarksDeclined_WhenAuthorized()
    {
        var approver = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var admin = GroupAdmin.Factory.Create(approver.Id, group.Id);
        var requestor = new UserDataBuilder().Build();

        SeedAccessContext.AddRange(approver, requestor, group, admin);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = GroupAssignmentRequest.Factory.Create(requestor.Id, group.Id, DateTime.UtcNow);
        SeedAccessContext.GroupAssignmentRequests.Add(request);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new DeclineAccessRequestCommand { RequestId = request.Id, IsGroup = true, UserId = approver.Id }, TestContext.Current.CancellationToken);
        Assert.True(ok);

        SeedAccessContext.ChangeTracker.Clear();
        var updatedRequest = await SeedAccessContext.GroupAssignmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest.Approved == false);
        Assert.Equal(approver.Id, updatedRequest.ManagedBy);
        Assert.False(await SeedAccessContext.AssignedToGroups.AnyAsync(a => a.GroupId == group.Id && a.UserId == requestor.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeclineAccessRequest_Event_ReturnsFalse_WhenUnauthorized()
    {
        var owner = new UserDataBuilder().Build();
        var outsider = new UserDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(owner).Build();

        SeedAccessContext.AddRange(owner, outsider, requestor, evt);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = EventAssignmentRequest.Factory.Create(requestor.Id, evt.Id);
        SeedAccessContext.EventAssignmentRequests.Add(request);
        await SeedAccessContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await Send(new DeclineAccessRequestCommand { RequestId = request.Id, IsGroup = false, UserId = outsider.Id }, TestContext.Current.CancellationToken);
        Assert.False(ok);

        SeedAccessContext.ChangeTracker.Clear();
        var unchanged = await SeedAccessContext.EventAssignmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.Null(unchanged.Approved);
        Assert.Null(unchanged.ManagedBy);
        Assert.False(await SeedAccessContext.AssignedToEvents.AnyAsync(a => a.EventId == evt.Id && a.UserId == requestor.Id, TestContext.Current.CancellationToken));
    }
}
