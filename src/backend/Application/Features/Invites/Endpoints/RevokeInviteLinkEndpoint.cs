using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Revokes an invite link. Caller must be a current admin of the link's target group/event, not
/// only its creator (FR-007). A no-op (409) when the link was already redeemed.
/// </summary>
public class RevokeInviteLinkEndpoint : EndpointWithoutRequest
{
    private readonly IInviteLinkService inviteLinkService;

    public RevokeInviteLinkEndpoint(IInviteLinkService inviteLinkService)
    {
        this.inviteLinkService = inviteLinkService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.InviteLink.Revoke);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var linkId = Route<string>("linkId") ?? string.Empty;
        var userId = User.GetSubject();

        var result = await inviteLinkService.RevokeAsync(linkId, userId, ct);

        switch (result)
        {
            case RevokeInviteLinkResult.Revoked:
                await Send.NoContentAsync(ct);
                return;
            case RevokeInviteLinkResult.AlreadyRedeemed:
                AddError("This invite link was already redeemed; revoking it has no effect.");
                await Send.ErrorsAsync(409, ct);
                return;
            case RevokeInviteLinkResult.NotAuthorized:
                await Send.ForbiddenAsync(ct);
                return;
            case RevokeInviteLinkResult.NotFound:
            default:
                await Send.NotFoundAsync(ct);
                return;
        }
    }
}
