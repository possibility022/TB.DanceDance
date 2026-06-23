using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Creates an invite link for an event. Caller must be the event's owner/admin (FR-002, FR-003).
/// </summary>
public class CreateEventInviteLinkEndpoint : EndpointWithoutRequest<InviteLinkResponse>
{
    private readonly IInviteLinkService inviteLinkService;
    private readonly IOptions<AppOptions> appOptions;

    public CreateEventInviteLinkEndpoint(IInviteLinkService inviteLinkService, IOptions<AppOptions> appOptions)
    {
        this.inviteLinkService = inviteLinkService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Events.InviteLinks);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var eventId = Route<Guid>("eventId");
        var userId = User.GetSubject();

        try
        {
            var link = await inviteLinkService.CreateForEventAsync(eventId, userId, ct);
            await Send.OkAsync(InviteLinkMapper.MapToResponse(link, appOptions.Value.AppWebsiteOrigin), ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
