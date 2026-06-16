using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Revokes a pending transfer. Sender-only. Requires authentication.
/// </summary>
public class RevokeTransferEndpoint : EndpointWithoutRequest
{
    private readonly ITransferService transferService;

    public RevokeTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Transfer.Revoke);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        var result = await transferService.RevokeTransferAsync(linkId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
