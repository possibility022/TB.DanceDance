using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Creates an invite link for a group. Caller must be a current admin of the group (FR-001, FR-003).
/// </summary>
public class CreateGroupInviteLinkEndpoint : EndpointWithoutRequest<InviteLinkResponse>
{
    private readonly IInviteLinkService inviteLinkService;
    private readonly IOptions<AppOptions> appOptions;

    public CreateGroupInviteLinkEndpoint(IInviteLinkService inviteLinkService, IOptions<AppOptions> appOptions)
    {
        this.inviteLinkService = inviteLinkService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Groups.InviteLinks);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var userId = User.GetSubject();

        try
        {
            var link = await inviteLinkService.CreateForGroupAsync(groupId, userId, ct);
            await Send.OkAsync(InviteLinkMapper.MapToResponse(link, appOptions.Value.AppWebsiteOrigin), ct);
        }
        catch (UnauthorizedAccessException)
        {
            await Send.ForbiddenAsync(ct);
        }
    }
}
