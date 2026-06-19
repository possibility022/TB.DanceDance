using System.Security.Claims;
using Application.Features.Groups;
using Application.Features.Groups.Endpoints;
using Application.Features.Videos;
using Domain.Entities;
using FastEndpoints;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TB.DanceDance.API.Contracts.Features.Groups;
using TB.DanceDance.Tests.TestsFixture;
using Group = Domain.Entities.Group;

namespace TB.DanceDance.Tests.Features.Groups;

/// <summary>
/// Endpoint-level tests driving each group endpoint's HandleAsync via FastEndpoints' Factory.Create
/// with a real GroupService over the Testcontainers DB. Focus: the IsGroupAdmin gate (403) and the
/// status-code mapping (200/204/404/409).
/// </summary>
public class GroupManagementEndpointTests : BaseTestClass
{
    private IGroupService groupService = null!;

    public GroupManagementEndpointTests(DanceDbFixture dbContextFixture) : base(dbContextFixture)
    {
    }

    protected override ValueTask Initialize(DanceDbContext runtimeDbContext)
    {
        groupService = new GroupService(runtimeDbContext, Substitute.For<IThumbnailUrlService>());
        return ValueTask.CompletedTask;
    }

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DefaultHttpContext Ctx(string? sub, params (string Key, object Value)[] routeValues)
    {
        var ctx = new DefaultHttpContext();
        if (sub != null)
        {
            var identity = new ClaimsIdentity([new Claim("sub", sub)], "test");
            ctx.User = new ClaimsPrincipal(identity);
        }
        foreach (var (key, value) in routeValues)
            ctx.Request.RouteValues[key] = value;
        return ctx;
    }

    /// <summary>Seeds a group with one admin and one plain member; returns (group, admin, member).</summary>
    private async Task<(Group group, User admin, User member)> SeedGroupWithAdminAndMember()
    {
        var memberB = new UserDataBuilder();
        var admin = new UserDataBuilder().Build();
        var member = memberB.Build();
        var group = new GroupDataBuilder().Build();
        SeedDbContext.AddRange(admin, member, group,
            new GroupAdmin { Id = Guid.NewGuid(), UserId = admin.Id, GroupId = group.Id },
            memberB.AssignTo(group, DateTime.UtcNow));
        await SeedDbContext.SaveChangesAsync(Ct);
        return (group, admin, member);
    }

    [Fact]
    public async Task CreateGroup_RecordsCreatorAsAdmin_Returns200()
    {
        var creator = new UserDataBuilder().Build();
        SeedDbContext.Add(creator);
        await SeedDbContext.SaveChangesAsync(Ct);

        var ep = Factory.Create<CreateGroupEndpoint>(Ctx(creator.Id), groupService);
        await ep.HandleAsync(
            new CreateGroupRequest { Name = "Intermediate", SeasonStart = new DateTime(2024, 9, 1), SeasonEnd = new DateTime(2025, 8, 31) },
            Ct);

        Assert.Equal(200, ep.HttpContext.Response.StatusCode);
        Assert.NotEqual(Guid.Empty, ep.Response.Id);
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == ep.Response.Id && ga.UserId == creator.Id, Ct));
    }

    [Fact]
    public async Task ListAdmins_NonAdmin_Returns403()
    {
        var (group, _, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<ListGroupAdminsEndpoint>(Ctx(member.Id, ("groupId", group.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ListMembers_NonAdmin_Returns403()
    {
        var (group, _, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<ListGroupMembersEndpoint>(Ctx(member.Id, ("groupId", group.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task AddAdmin_NonAdmin_Returns403()
    {
        var (group, _, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<AddGroupAdminEndpoint>(Ctx(member.Id, ("groupId", group.Id)), groupService);
        await ep.HandleAsync(new AddGroupAdminRequest { UserId = member.Id }, Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RemoveAdmin_NonAdmin_Returns403()
    {
        var (group, admin, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<RemoveGroupAdminEndpoint>(Ctx(member.Id, ("groupId", group.Id), ("userId", admin.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task UpdateMember_NonAdmin_Returns403()
    {
        var (group, _, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<UpdateGroupMemberEndpoint>(Ctx(member.Id, ("groupId", group.Id), ("userId", member.Id)), groupService);
        await ep.HandleAsync(new UpdateGroupMemberRequest { WhenJoined = new DateTime(2024, 1, 1) }, Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_NonAdmin_Returns403()
    {
        var (group, _, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<RemoveGroupMemberEndpoint>(Ctx(member.Id, ("groupId", group.Id), ("userId", member.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(403, ep.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task AddAdmin_AsAdmin_Returns204_AndGrantsAdmin()
    {
        var (group, admin, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<AddGroupAdminEndpoint>(Ctx(admin.Id, ("groupId", group.Id)), groupService);
        await ep.HandleAsync(new AddGroupAdminRequest { UserId = member.Id }, Ct);

        Assert.Equal(204, ep.HttpContext.Response.StatusCode);
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == group.Id && ga.UserId == member.Id, Ct));
    }

    [Fact]
    public async Task RemoveAdmin_LastAdmin_Returns409()
    {
        var (group, admin, _) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<RemoveGroupAdminEndpoint>(Ctx(admin.Id, ("groupId", group.Id), ("userId", admin.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(409, ep.HttpContext.Response.StatusCode);
        Assert.True(await SeedDbContext.GroupsAdmins.AnyAsync(ga => ga.GroupId == group.Id && ga.UserId == admin.Id, Ct));
    }

    [Fact]
    public async Task UpdateMember_AsAdmin_Returns204_AndChangesJoinDate()
    {
        var (group, admin, member) = await SeedGroupWithAdminAndMember();
        var newDate = DateTime.SpecifyKind(new DateTime(2024, 3, 4), DateTimeKind.Utc);

        var ep = Factory.Create<UpdateGroupMemberEndpoint>(Ctx(admin.Id, ("groupId", group.Id), ("userId", member.Id)), groupService);
        await ep.HandleAsync(new UpdateGroupMemberRequest { WhenJoined = newDate }, Ct);

        Assert.Equal(204, ep.HttpContext.Response.StatusCode);
        SeedDbContext.ChangeTracker.Clear();
        var membership = await SeedDbContext.AssingedToGroups.SingleAsync(a => a.GroupId == group.Id && a.UserId == member.Id, Ct);
        Assert.Equal(newDate, membership.WhenJoined);
    }

    [Fact]
    public async Task RemoveMember_AsAdmin_Returns204_AndDeletesMembership()
    {
        var (group, admin, member) = await SeedGroupWithAdminAndMember();

        var ep = Factory.Create<RemoveGroupMemberEndpoint>(Ctx(admin.Id, ("groupId", group.Id), ("userId", member.Id)), groupService);
        await ep.HandleAsync(Ct);

        Assert.Equal(204, ep.HttpContext.Response.StatusCode);
        SeedDbContext.ChangeTracker.Clear();
        Assert.False(await SeedDbContext.AssingedToGroups.AnyAsync(a => a.GroupId == group.Id && a.UserId == member.Id, Ct));
    }
}
