using Application.Features.Groups;
using Application.Features.Videos;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Groups;

/// <summary>
/// Service-level tests for group creation, admin management (with the last-admin guard),
/// member management, and the admin-bootstrap SQL shared with the BootstrapGroupAdmins migration.
/// </summary>
public class GroupManagementServiceTests : BaseTestClass
{
    private GroupService groupService = null!;

    public GroupManagementServiceTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        groupService = new GroupService(runtimeDbContext, Substitute.For<IThumbnailUrlService>());
        return ValueTask.CompletedTask;
    }

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateGroupAsync_RecordsCreatorAsAdmin()
    {
        var creator = new UserDataBuilder().Build();
        SeedDbContext.Add(creator);
        await SeedDbContext.SaveChangesAsync(Ct);

        var group = await groupService.CreateGroupAsync(
            "Beginners", new DateOnly(2024, 9, 1), new DateOnly(2025, 8, 31), creator.Id, Ct);

        SeedDbContext.ChangeTracker.Clear();
        var savedGroup = await SeedDbContext.Groups.FirstOrDefaultAsync(g => g.Id == group.Id, Ct);
        Assert.NotNull(savedGroup);
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == group.Id && ga.UserId == creator.Id, Ct));
        Assert.True(await groupService.IsGroupAdmin(group.Id, creator.Id, Ct));
    }

    [Fact]
    public async Task GetAdministeredGroupIdsAsync_ReturnsOnlyGroupsWhereUserIsAdmin()
    {
        var user = new UserDataBuilder().Build();
        var administered = new GroupDataBuilder().Build();
        var other = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(user, administered, other,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = user.Id, GroupId = administered.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        var ids = await groupService.GetAdministeredGroupIdsAsync(user.Id, Ct);

        Assert.Equal([administered.Id], ids);
    }

    [Fact]
    public async Task GetAdministeredGroupsAsync_ReturnsOnlyGroupsWhereUserIsAdmin()
    {
        var user = new UserDataBuilder().Build();
        var administered = new GroupDataBuilder().Build();
        var other = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(user, administered, other,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = user.Id, GroupId = administered.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        var groups = await groupService.GetAdministeredGroupsAsync(user.Id, Ct);

        var group = Assert.Single(groups);
        Assert.Equal(administered.Id, group.Id);
        Assert.Equal(administered.Name, group.Name);
    }

    [Fact]
    public async Task GetAdministeredGroupsAsync_ReturnsEmpty_WhenUserAdministersNoGroups()
    {
        var user = new UserDataBuilder().Build();
        SeedDbContext.Add(user);
        await SeedDbContext.SaveChangesAsync(Ct);

        var groups = await groupService.GetAdministeredGroupsAsync(user.Id, Ct);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task AddAdminAsync_IsIdempotent_AndRejectsUnknownUser()
    {
        var admin = new UserDataBuilder().Build();
        var newAdmin = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin, newAdmin, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin.Id, GroupId = group.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        Assert.False(await groupService.AddAdminAsync(group.Id, "does-not-exist", Ct));

        Assert.True(await groupService.AddAdminAsync(group.Id, newAdmin.Id, Ct));
        Assert.True(await groupService.AddAdminAsync(group.Id, newAdmin.Id, Ct)); // idempotent

        SeedDbContext.ChangeTracker.Clear();
        var count = await SeedDbContext.GroupsAdmins.CountAsync(ga => ga.GroupId == group.Id && ga.UserId == newAdmin.Id, Ct);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveAdminAsync_RemovesOtherAdmin_WhenMoreThanOneRemains()
    {
        var admin1 = new UserDataBuilder().Build();
        var admin2 = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin1, admin2, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin1.Id, GroupId = group.Id },
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin2.Id, GroupId = group.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        var result = await groupService.RemoveAdminAsync(group.Id, admin2.Id, Ct);

        Assert.Equal(RemoveGroupAdminResult.Removed, result);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == group.Id && ga.UserId == admin2.Id, Ct));
    }

    [Fact]
    public async Task RemoveAdminAsync_BlocksRemovingTheLastAdmin()
    {
        var admin = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin.Id, GroupId = group.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        // Self-removal of the only admin is the last-admin case and must be blocked.
        var result = await groupService.RemoveAdminAsync(group.Id, admin.Id, Ct);

        Assert.Equal(RemoveGroupAdminResult.BlockedLastAdmin, result);
        SeedDbContext.ChangeTracker.Clear();
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == group.Id && ga.UserId == admin.Id, Ct));
    }

    [Fact]
    public async Task RemoveAdminAsync_AllowsSelfRemoval_WhenAnotherAdminRemains()
    {
        var self = new UserDataBuilder().Build();
        var other = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(self, other, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = self.Id, GroupId = group.Id },
            new GroupAdmin { Id = Guid.NewGuid(), UserId = other.Id, GroupId = group.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        var result = await groupService.RemoveAdminAsync(group.Id, self.Id, Ct);

        Assert.Equal(RemoveGroupAdminResult.Removed, result);
    }

    [Fact]
    public async Task RemoveAdminAsync_ReturnsNotAnAdmin_WhenTargetIsNotAdmin()
    {
        var admin = new UserDataBuilder().Build();
        var stranger = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin, stranger, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin.Id, GroupId = group.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        Assert.Equal(RemoveGroupAdminResult.NotAnAdmin, await groupService.RemoveAdminAsync(group.Id, stranger.Id, Ct));
    }

    [Fact]
    public async Task UpdateMemberJoinedAsync_ChangesJoinDate_AndReturnsFalseForNonMember()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joined = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-10), DateTimeKind.Utc);
        SeedDbContext.AddRange(user, group, userB.AssignTo(group, joined));
        await SeedDbContext.SaveChangesAsync(Ct);

        var newDate = DateTime.SpecifyKind(new DateTime(2024, 1, 2), DateTimeKind.Utc);
        Assert.True(await groupService.UpdateMemberJoinedAsync(group.Id, user.Id, newDate, Ct));
        Assert.False(await groupService.UpdateMemberJoinedAsync(group.Id, "not-a-member", newDate, Ct));

        SeedDbContext.ChangeTracker.Clear();
        var membership = await SeedDbContext.AssingedToGroups.SingleAsync(a => a.GroupId == group.Id && a.UserId == user.Id, Ct);
        Assert.Equal(newDate, membership.WhenJoined);
    }

    [Fact]
    public async Task RemoveMemberAsync_DeletesMembership_AndReturnsFalseForNonMember()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(user, group, userB.AssignTo(group, DateTime.UtcNow));
        await SeedDbContext.SaveChangesAsync(Ct);

        Assert.False(await groupService.RemoveMemberAsync(group.Id, "not-a-member", Ct));
        Assert.True(await groupService.RemoveMemberAsync(group.Id, user.Id, Ct));

        SeedDbContext.ChangeTracker.Clear();
        Assert.False(await SeedDbContext.AssingedToGroups.AnyAsync(a => a.GroupId == group.Id && a.UserId == user.Id, Ct));
    }

    [Fact]
    public async Task GetMembersAsync_ReturnsMembersWithUserDetailsAndJoinDate()
    {
        var userB = new UserDataBuilder();
        var user = userB.Build();
        var group = new GroupDataBuilder().Build();
        var joined = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc);
        SeedDbContext.AddRange(user, group, userB.AssignTo(group, joined));
        await SeedDbContext.SaveChangesAsync(Ct);

        var members = await groupService.GetMembersAsync(group.Id, Ct);

        var member = Assert.Single(members);
        Assert.Equal(user.Id, member.UserId);
        Assert.Equal(user.Email, member.Email);
        // Postgres timestamptz keeps microsecond precision, so allow a tiny tolerance.
        Assert.True((member.WhenJoined - joined).Duration() < TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public async Task BootstrapSql_CreatesOneAdminPerMemberPair_AndIsIdempotent()
    {
        var u1 = new UserDataBuilder();
        var u2 = new UserDataBuilder();
        var user1 = u1.Build();
        var user2 = u2.Build();
        var groupA = new GroupDataBuilder().Build();
        var groupB = new GroupDataBuilder().Build();

        SeedDbContext.AddRange(user1, user2, groupA, groupB,
            u1.AssignTo(groupA, DateTime.UtcNow),
            u2.AssignTo(groupA, DateTime.UtcNow),
            u1.AssignTo(groupB, DateTime.UtcNow));
        // groupA already has user1 as admin: the bootstrap must not duplicate it.
        SeedDbContext.Add(new GroupAdmin { Id = Guid.NewGuid(), UserId = user1.Id, GroupId = groupA.Id });
        await SeedDbContext.SaveChangesAsync(Ct);

        await SeedDbContext.Database.ExecuteSqlRawAsync(GroupAdminBootstrap.Sql, Ct);

        SeedDbContext.ChangeTracker.Clear();
        // Scope counts to these two groups: the per-class DB is shared with sibling tests.
        // Pairs: (A,u1) already admin, (A,u2) new, (B,u1) new => 2 in A, 1 in B.
        var afterFirst = await CountAdminsAsync(groupA.Id, groupB.Id);
        Assert.Equal((2, 1), afterFirst);
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == groupA.Id && ga.UserId == user2.Id, Ct));
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == groupB.Id && ga.UserId == user1.Id, Ct));

        // Re-running adds nothing.
        await SeedDbContext.Database.ExecuteSqlRawAsync(GroupAdminBootstrap.Sql, Ct);
        SeedDbContext.ChangeTracker.Clear();
        Assert.Equal(afterFirst, await CountAdminsAsync(groupA.Id, groupB.Id));
    }

    private async Task<(int A, int B)> CountAdminsAsync(Guid groupA, Guid groupB)
    {
        var a = await SeedDbContext.GroupsAdmins.CountAsync(ga => ga.GroupId == groupA, Ct);
        var b = await SeedDbContext.GroupsAdmins.CountAsync(ga => ga.GroupId == groupB, Ct);
        return (a, b);
    }
}
