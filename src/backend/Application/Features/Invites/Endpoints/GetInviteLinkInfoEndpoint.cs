using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Invites;

namespace Application.Features.Invites.Endpoints;

/// <summary>
/// Gets the public preview of an invite link, for the landing page to show before the visitor
/// signs in (FR-005, FR-012). Anonymous access allowed.
/// </summary>
public class GetInviteLinkInfoEndpoint : EndpointWithoutRequest<InviteLinkInfoResponse>
{
    private readonly IInviteLinkService inviteLinkService;

    public GetInviteLinkInfoEndpoint(IInviteLinkService inviteLinkService)
    {
        this.inviteLinkService = inviteLinkService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.InviteLink.GetInfo);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var linkId = Route<string>("linkId") ?? string.Empty;
        var info = await inviteLinkService.GetInfoAsync(linkId, ct);

        if (info == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new InviteLinkInfoResponse
        {
            Id = info.Id,
            TargetType = info.TargetType,
            TargetName = info.TargetName,
            IsRedeemable = info.IsRedeemable,
        }, ct);
    }
}
