using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Lists an event's invite links. Caller must be the event's owner/admin (FR-008).
/// </summary>
public class ListEventInviteLinksEndpoint : EndpointWithoutRequest<ListInviteLinksResponse>
{
    private readonly IInviteLinkService inviteLinkService;
    private readonly IOptions<AppOptions> appOptions;

    public ListEventInviteLinksEndpoint(IInviteLinkService inviteLinkService, IOptions<AppOptions> appOptions)
    {
        this.inviteLinkService = inviteLinkService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Events.InviteLinks);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var eventId = Route<Guid>("eventId");
        var userId = User.GetSubject();

        try
        {
            var links = await inviteLinkService.ListForEventAsync(eventId, userId, ct);
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
