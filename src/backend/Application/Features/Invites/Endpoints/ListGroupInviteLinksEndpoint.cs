using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Lists a group's invite links. Caller must be a current admin of the group, regardless of which
/// admin created any given link (FR-008).
/// </summary>
public class ListGroupInviteLinksEndpoint : EndpointWithoutRequest<ListInviteLinksResponse>
{
    private readonly IInviteLinkService inviteLinkService;
    private readonly IOptions<AppOptions> appOptions;

    public ListGroupInviteLinksEndpoint(IInviteLinkService inviteLinkService, IOptions<AppOptions> appOptions)
    {
        this.inviteLinkService = inviteLinkService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.InviteLinks);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var userId = User.GetSubject();

        try
        {
            var links = await inviteLinkService.ListForGroupAsync(groupId, userId, ct);
            var origin = appOptions.Value.AppWebsiteOrigin;

            await Send.OkAsync(new ListInviteLinksResponse
            {
                InviteLinks = links.Select(l => InviteLinkMapper.MapToResponse(l, origin)).ToArray(),
            }, ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
