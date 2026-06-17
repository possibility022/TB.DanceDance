using Application.Extensions;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Owner cancels a transfer that the recipient has already accepted. Ownership is unchanged;
/// the transfer moves to Cancelled. Sender-only.
/// </summary>
public class CancelTransferEndpoint : EndpointWithoutRequest
{
    private readonly ITransferService transferService;

    public CancelTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Cancel);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        var cancelled = await transferService.CancelTransferAsync(linkId, userId, ct);

        if (!cancelled)
            return await Send.NotFoundAsync(ct);

        return await Send.NoContentAsync(ct);
    }
}
