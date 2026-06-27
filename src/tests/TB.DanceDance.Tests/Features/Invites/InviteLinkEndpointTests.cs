using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application;
using Application.Features.Invites;
using Domain.Entities;
using FastEndpoints.Testing;
using TB.DanceDance.API.Contracts.Features.Invites;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Features.Invites;

/// <summary>
/// End-to-end coverage of the invite-links feature through the real API host (Testcontainers
/// Postgres, real FastEndpoints auth policies) — the only way to observe true 401/403 pipeline
/// behavior and the anonymous info endpoint. Service-level redemption/concurrency/admin-check
/// behavior is already covered by <see cref="InviteLinkServiceTests"/> and is not repeated here.
/// </summary>
public class InviteLinkEndpointTests(WebAppFixture App) : TestBaseWithAssemblyFixture<WebAppFixture>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string GroupInviteLinksRoute(Guid groupId) =>
        ApiRoutes.Groups.InviteLinks.Replace("{groupId:guid}", groupId.ToString());

    private static string EventInviteLinksRoute(Guid eventId) =>
        ApiRoutes.Events.InviteLinks.Replace("{eventId:guid}", eventId.ToString());

    private static string GetInfoRoute(string linkId) =>
        ApiRoutes.InviteLink.GetInfo.Replace("{linkId}", linkId);

    private static string RedeemRoute(string linkId) =>
        ApiRoutes.InviteLink.Redeem.Replace("{linkId}", linkId);

    private static string RevokeRoute(string linkId) =>
        ApiRoutes.InviteLink.Revoke.Replace("{linkId}", linkId);

    private async Task<(User admin, Group group)> SeedGroupWithAdmin()
    {
        var admin = new UserDataBuilder().Build();
        var group = new GroupDataBuilder().Build();
        await using var db = App.CreateDbContext();
        db.AddRange(admin, group);
        db.GroupsAdmins.Add(new GroupAdmin { Id = Guid.NewGuid(), GroupId = group.Id, UserId = admin.Id });
        await db.SaveChangesAsync(Cancellation);
        return (admin, group);
    }

    private async Task<(User owner, Event evt)> SeedEventWithOwner()
    {
        var owner = new UserDataBuilder().Build();
        var evt = new EventDataBuilder().WithOwner(owner).Build();
        await using var db = App.CreateDbContext();
        db.AddRange(owner, evt);
        await db.SaveChangesAsync(Cancellation);
        return (owner, evt);
    }

    private async Task<InviteLink> SeedActiveLinkForGroup(User admin, Group group)
    {
        var link = new InviteLinkDataBuilder().ForGroup(group).CreatedBy(admin).Build();
        await using var db = App.CreateDbContext();
        db.Add(link);
        await db.SaveChangesAsync(Cancellation);
        return link;
    }

    [Fact]
    public async Task CreateGroupInviteLink_Admin_ReturnsOkWithUrl()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var client = App.CreateAuthorizedClient(admin.Id, ApiScopes.Read);

        var response = await client.PostAsync(GroupInviteLinksRoute(group.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InviteLinkResponse>(JsonOptions, Cancellation);
        Assert.NotNull(body);
        Assert.Equal(group.Id, body!.GroupId);
        Assert.Equal("Active", body.Status);
        Assert.Contains($"/invite/{body.Id}", body.Url);
    }

    [Fact]
    public async Task CreateGroupInviteLink_NonAdmin_ReturnsForbidden()
    {
        var (_, group) = await SeedGroupWithAdmin();
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.PostAsync(GroupInviteLinksRoute(group.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEventInviteLink_Owner_ReturnsOk()
    {
        var (owner, evt) = await SeedEventWithOwner();
        var client = App.CreateAuthorizedClient(owner.Id, ApiScopes.Read);

        var response = await client.PostAsync(EventInviteLinksRoute(evt.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InviteLinkResponse>(JsonOptions, Cancellation);
        Assert.Equal(evt.Id, body!.EventId);
    }

    [Fact]
    public async Task CreateEventInviteLink_NonOwner_ReturnsForbidden()
    {
        var (_, evt) = await SeedEventWithOwner();
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.PostAsync(EventInviteLinksRoute(evt.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInviteLinkInfo_Anonymous_ReturnsOkWithPreview()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var client = App.CreateAnonymousClient();

        var response = await client.GetAsync(GetInfoRoute(link.Id), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InviteLinkInfoResponse>(JsonOptions, Cancellation);
        Assert.NotNull(body);
        Assert.Equal("Group", body!.TargetType);
        Assert.Equal(group.Name, body.TargetName);
        Assert.True(body.IsRedeemable);
    }

    [Fact]
    public async Task GetInviteLinkInfo_UnknownId_ReturnsNotFound()
    {
        var client = App.CreateAnonymousClient();

        var response = await client.GetAsync(GetInfoRoute("doesnotexist"), Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RedeemInviteLink_SignedOut_ReturnsUnauthorized()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var client = App.CreateAnonymousClient();

        var response = await client.PostAsync(RedeemRoute(link.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RedeemInviteLink_SignedIn_ReturnsOk()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var redeemer = new UserDataBuilder().Build();
        await using (var db = App.CreateDbContext())
        {
            db.Add(redeemer);
            await db.SaveChangesAsync(Cancellation);
        }
        var client = App.CreateAuthorizedClient(redeemer.Id, ApiScopes.Read);

        var response = await client.PostAsync(RedeemRoute(link.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RedeemInviteLinkResponse>(JsonOptions, Cancellation);
        Assert.False(body!.AlreadyMember);
    }

    [Fact]
    public async Task RedeemInviteLink_AlreadyRedeemed_ReturnsConflict()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var firstRedeemer = new UserDataBuilder().Build();
        var secondRedeemer = new UserDataBuilder().Build();
        var link = new InviteLinkDataBuilder().ForGroup(group).CreatedBy(admin).RedeemedBy(firstRedeemer).Build();
        await using (var db = App.CreateDbContext())
        {
            db.AddRange(firstRedeemer, secondRedeemer, link);
            await db.SaveChangesAsync(Cancellation);
        }
        var client = App.CreateAuthorizedClient(secondRedeemer.Id, ApiScopes.Read);

        var response = await client.PostAsync(RedeemRoute(link.Id), content: null, Cancellation);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ListGroupInviteLinks_NonAdmin_ReturnsForbidden()
    {
        var (_, group) = await SeedGroupWithAdmin();
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.GetAsync(GroupInviteLinksRoute(group.Id), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListGroupInviteLinks_AnyCurrentAdmin_ReturnsLinks()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var client = App.CreateAuthorizedClient(admin.Id, ApiScopes.Read);

        var response = await client.GetAsync(GroupInviteLinksRoute(group.Id), Cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListInviteLinksResponse>(JsonOptions, Cancellation);
        Assert.Contains(body!.InviteLinks, l => l.Id == link.Id);
    }

    [Fact]
    public async Task RevokeInviteLink_Admin_ReturnsNoContent()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var client = App.CreateAuthorizedClient(admin.Id, ApiScopes.Read);

        var response = await client.DeleteAsync(RevokeRoute(link.Id), Cancellation);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInviteLink_AlreadyRedeemed_ReturnsConflict()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var redeemer = new UserDataBuilder().Build();
        var link = new InviteLinkDataBuilder().ForGroup(group).CreatedBy(admin).RedeemedBy(redeemer).Build();
        await using (var db = App.CreateDbContext())
        {
            db.AddRange(redeemer, link);
            await db.SaveChangesAsync(Cancellation);
        }
        var client = App.CreateAuthorizedClient(admin.Id, ApiScopes.Read);

        var response = await client.DeleteAsync(RevokeRoute(link.Id), Cancellation);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInviteLink_NonAdmin_ReturnsForbidden()
    {
        var (admin, group) = await SeedGroupWithAdmin();
        var link = await SeedActiveLinkForGroup(admin, group);
        var client = App.CreateAuthorizedClient(TestDataBuilder.RandomUserId(), ApiScopes.Read);

        var response = await client.DeleteAsync(RevokeRoute(link.Id), Cancellation);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
