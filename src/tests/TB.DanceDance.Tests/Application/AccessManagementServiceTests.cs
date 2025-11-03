using Application.Services;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace TB.DanceDance.Tests.Application;

public class AccessManagementServiceTests : BaseTestClass
{
    private AccessManagementService service = null!;

    public AccessManagementServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        service = new AccessManagementService(runtimeDbContext);
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SaveEventsAssigmentRequest_AddsRequests_AndSkipsPendingDuplicates()
    {
        var requestor = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var evtB1 = new EventDataBuilder().WithOwner(owner);
        var evt1 = evtB1.Build();
        var evtB2 = new EventDataBuilder().WithOwner(owner);
        var evt2 = evtB2.Build();

        SeedDbContext.AddRange(owner, requestor, evt1, evt2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.SaveEventsAssigmentRequest(requestor.Id, [evt1.Id, evt2.Id], TestContext.Current.CancellationToken);
        // Attempt to add duplicates again
        await service.SaveEventsAssigmentRequest(requestor.Id, [evt1.Id, evt2.Id], TestContext.Current.CancellationToken);

        var all = SeedDbContext.EventAssigmentRequests.Where(r => r.UserId == requestor.Id).ToList();
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

        SeedDbContext.AddRange(requestor, group1, group2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.SaveGroupsAssigmentRequests(requestor.Id,
            [(group1.Id, joined1), (group2.Id, joined2)], TestContext.Current.CancellationToken);
        // Attempt duplicates
        await service.SaveGroupsAssigmentRequests(requestor.Id,
            [(group1.Id, joined1), (group2.Id, joined2)], TestContext.Current.CancellationToken);

        var all = SeedDbContext.GroupAssigmentRequests.Where(r => r.UserId == requestor.Id).ToList();
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
        var userB = new UserDataBuilder();
        var user = userB.Build();

        await service.AddOrUpdateUserAsync(user, TestContext.Current.CancellationToken);
        var saved = await SeedDbContext.Users.FindAsync([user.Id], cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(saved);

        // Update using a new instance to avoid tracking quirks across contexts
        var updatedUser = new User
        {
            Id = user.Id,
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            Email = user.Email + ".ignored"
        };

        await service.AddOrUpdateUserAsync(updatedUser, TestContext.Current.CancellationToken);
        // Clear tracking to ensure we read the latest values from the database
        SeedDbContext.ChangeTracker.Clear();
        var updated = await SeedDbContext.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id, TestContext.Current.CancellationToken);
        Assert.Equal("UpdatedFirst", updated!.FirstName);
        Assert.Equal("UpdatedLast", updated!.LastName);
    }

    [Fact]
    public async Task GetPendingUserRequests_ReturnsPendingAndRejected_ExcludesApproved()
    {
        var user = new UserDataBuilder().Build();
        var owner = new UserDataBuilder().Build();
        var evtB1 = new EventDataBuilder().WithOwner(owner);
        var evt1 = evtB1.Build();
        var evtB2 = new EventDataBuilder().WithOwner(owner);
        var evt2 = evtB2.Build();
        var group1 = new GroupDataBuilder().Build();
        var group2 = new GroupDataBuilder().Build();

        SeedDbContext.AddRange(owner, user, evt1, evt2, group1, group2);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        SeedDbContext.EventAssigmentRequests.AddRange(
            new EventAssigmentRequest { Id = Guid.NewGuid(), EventId = evt1.Id, UserId = user.Id, Approved = null },
            new EventAssigmentRequest { Id = Guid.NewGuid(), EventId = evt2.Id, UserId = user.Id, Approved = true }
        );
        SeedDbContext.GroupAssigmentRequests.AddRange(
            new GroupAssigmentRequest { Id = Guid.NewGuid(), GroupId = group1.Id, UserId = user.Id, Approved = false, WhenJoined = DateTime.UtcNow },
            new GroupAssigmentRequest { Id = Guid.NewGuid(), GroupId = group2.Id, UserId = user.Id, Approved = true, WhenJoined = DateTime.UtcNow }
        );
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var pending = await service.GetPendingUserRequests(user.Id, TestContext.Current.CancellationToken);
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
        var evtB = new EventDataBuilder().WithOwner(approver);
        var evt = evtB.Build();

        // approver is group admin
        var groupAdmin = new GroupAdmin { Id = Guid.NewGuid(), UserId = approver.Id, GroupId = group.Id };

        SeedDbContext.AddRange(approver, requestor, group, groupAdmin, evt);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var whenJoined = DateTime.UtcNow.AddDays(-1);
        SeedDbContext.GroupAssigmentRequests.Add(new GroupAssigmentRequest
        {
            Id = Guid.NewGuid(), GroupId = group.Id, UserId = requestor.Id, Approved = null, WhenJoined = whenJoined
        });
        SeedDbContext.EventAssigmentRequests.Add(new EventAssigmentRequest
        {
            Id = Guid.NewGuid(), EventId = evt.Id, UserId = requestor.Id, Approved = null
        });
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var results = await service.GetAccessRequestsToApproveAsync(approver.Id, TestContext.Current.CancellationToken);
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
        var admin = new GroupAdmin { Id = Guid.NewGuid(), UserId = approver.Id, GroupId = group.Id };

        SeedDbContext.AddRange(approver, requestor, group, admin);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var whenJoined = DateTime.UtcNow.AddDays(-5);
        var request = new GroupAssigmentRequest { Id = Guid.NewGuid(), GroupId = group.Id, UserId = requestor.Id, Approved = null, WhenJoined = whenJoined };
        SeedDbContext.GroupAssigmentRequests.Add(request);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await service.ApproveAccessRequest(request.Id, isGroup: true, userId: approver.Id);
        Assert.True(ok);

        var membership = SeedDbContext.AssingedToGroups.Single(a => a.GroupId == group.Id && a.UserId == requestor.Id);
        Assert.True((membership.WhenJoined - whenJoined).Duration() < TimeSpan.FromMilliseconds(5));

        // Reload to avoid stale tracked entity
        var updatedRequest = await SeedDbContext.GroupAssigmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest!.Approved == true);
        Assert.Equal(approver.Id, updatedRequest!.ManagedBy);
    }

    [Fact]
    public async Task ApproveAccessRequest_Group_ReturnsFalse_WhenUnauthorized()
    {
        var approver = new UserDataBuilder().Build();
        var unauthorized = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var admin = new GroupAdmin { Id = Guid.NewGuid(), UserId = approver.Id, GroupId = group.Id };
        var requestor = new UserDataBuilder().Build();

        SeedDbContext.AddRange(approver, unauthorized, requestor, group, admin);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GroupAssigmentRequest { Id = Guid.NewGuid(), GroupId = group.Id, UserId = requestor.Id, Approved = null, WhenJoined = DateTime.UtcNow };
        SeedDbContext.GroupAssigmentRequests.Add(request);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await service.ApproveAccessRequest(request.Id, isGroup: true, userId: unauthorized.Id);
        Assert.False(ok);
        Assert.False(SeedDbContext.AssingedToGroups.Any(a => a.GroupId == group.Id && a.UserId == requestor.Id));
        var unchanged = await SeedDbContext.GroupAssigmentRequests.FindAsync([request.Id], TestContext.Current.CancellationToken);
        Assert.Null(unchanged!.Approved);
        Assert.Null(unchanged.ManagedBy);
    }

    [Fact]
    public async Task ApproveAccessRequest_Event_CreatesMembershipAndMarksApproved()
    {
        var owner = new UserDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var evtB = new EventDataBuilder().WithOwner(owner);
        var evt = evtB.Build();

        SeedDbContext.AddRange(owner, requestor, evt);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new EventAssigmentRequest { Id = Guid.NewGuid(), EventId = evt.Id, UserId = requestor.Id, Approved = null };
        SeedDbContext.EventAssigmentRequests.Add(request);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Ensure this request is approvable by the owner (matches service query)
        var approvables = await service.GetAccessRequestsToApproveAsync(owner.Id, TestContext.Current.CancellationToken);
        var approvableEventRequest = approvables.Single(r => !r.IsGroup && r.Name == evt.Name);

        var ok = await service.ApproveAccessRequest(approvableEventRequest.RequestId, isGroup: false, userId: owner.Id);
        Assert.True(ok);

        var membership = SeedDbContext.AssingedToEvents.Single(a => a.EventId == evt.Id && a.UserId == requestor.Id);
        var updatedRequest = await SeedDbContext.EventAssigmentRequests.AsNoTracking().FirstAsync(r => r.EventId == evt.Id && r.UserId == requestor.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest!.Approved == true);
        Assert.Equal(owner.Id, updatedRequest!.ManagedBy);
    }

    [Fact]
    public async Task DeclineAccessRequest_Group_MarksDeclined_WhenAuthorized()
    {
        var approver = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        var admin = new GroupAdmin { Id = Guid.NewGuid(), UserId = approver.Id, GroupId = group.Id };
        var requestor = new UserDataBuilder().Build();

        SeedDbContext.AddRange(approver, requestor, group, admin);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GroupAssigmentRequest { Id = Guid.NewGuid(), GroupId = group.Id, UserId = requestor.Id, Approved = null, WhenJoined = DateTime.UtcNow };
        SeedDbContext.GroupAssigmentRequests.Add(request);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await service.DeclineAccessRequest(request.Id, isGroup: true, userId: approver.Id, TestContext.Current.CancellationToken);
        Assert.True(ok);

        var updatedRequest = await SeedDbContext.GroupAssigmentRequests.AsNoTracking().FirstAsync(r => r.Id == request.Id, TestContext.Current.CancellationToken);
        Assert.True(updatedRequest!.Approved == false);
        Assert.Equal(approver.Id, updatedRequest!.ManagedBy);
        Assert.False(SeedDbContext.AssingedToGroups.Any(a => a.GroupId == group.Id && a.UserId == requestor.Id));
    }

    [Fact]
    public async Task DeclineAccessRequest_Event_ReturnsFalse_WhenUnauthorized()
    {
        var owner = new UserDataBuilder().Build();
        var outsider = new UserDataBuilder().Build();
        var requestor = new UserDataBuilder().Build();
        var evtB = new EventDataBuilder().WithOwner(owner);
        var evt = evtB.Build();

        SeedDbContext.AddRange(owner, outsider, requestor, evt);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new EventAssigmentRequest { Id = Guid.NewGuid(), EventId = evt.Id, UserId = requestor.Id, Approved = null };
        SeedDbContext.EventAssigmentRequests.Add(request);
        await SeedDbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ok = await service.DeclineAccessRequest(request.Id, isGroup: false, userId: outsider.Id, TestContext.Current.CancellationToken);
        Assert.False(ok);

        var unchanged = await SeedDbContext.EventAssigmentRequests.FindAsync([request.Id], TestContext.Current.CancellationToken);
        Assert.Null(unchanged!.Approved);
        Assert.Null(unchanged.ManagedBy);
        Assert.False(SeedDbContext.AssingedToEvents.Any(a => a.EventId == evt.Id && a.UserId == requestor.Id));
    }
}