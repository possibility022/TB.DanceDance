using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Redeems an invite link for the current user. Requires authentication (FR-012); first caller to
/// redeem wins (FR-004, FR-011).
/// </summary>
public class RedeemInviteLinkEndpoint : EndpointWithoutRequest<RedeemInviteLinkResponse>
{
    private readonly IInviteLinkService inviteLinkService;

    public RedeemInviteLinkEndpoint(IInviteLinkService inviteLinkService)
    {
        this.inviteLinkService = inviteLinkService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.InviteLink.Redeem);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var linkId = Route<string>("linkId") ?? string.Empty;
        var userId = User.GetSubject();

        var result = await inviteLinkService.RedeemAsync(linkId, userId, ct);

        switch (result)
        {
            case RedeemInviteLinkResult.Redeemed:
                await Send.OkAsync(new RedeemInviteLinkResponse { AlreadyMember = false }, ct);
                return;
            case RedeemInviteLinkResult.AlreadyMember:
                await Send.OkAsync(new RedeemInviteLinkResponse { AlreadyMember = true }, ct);
                return;
            case RedeemInviteLinkResult.NotAvailable:
            default:
                AddError("This invite link has already been used, expired, or been revoked.");
                await Send.ErrorsAsync(409, ct);
                return;
        }
    }
}
