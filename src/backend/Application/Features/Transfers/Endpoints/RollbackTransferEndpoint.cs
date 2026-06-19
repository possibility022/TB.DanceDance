using Application.Extensions;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Sender rolls back a transfer the recipient already accepted, within the rollback window.
/// Ownership and the private share rows move back to the sender; the sender's share links that
/// were revoked at acceptance time stay revoked. Sender-only.
/// </summary>
public class RollbackTransferEndpoint : EndpointWithoutRequest
{
    private readonly ITransferService transferService;

    public RollbackTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Rollback);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        var result = await transferService.RollbackTransferAsync(linkId, userId, ct);

        switch (result)
        {
            case RollbackTransferResult.RolledBack:
                return await Send.NoContentAsync(ct);
            case RollbackTransferResult.NotOwner:
                return await Send.ForbiddenAsync(ct);
            case RollbackTransferResult.WindowExpired:
                AddError("The rollback window for this transfer has expired.");
                return await Send.ErrorsAsync(409, ct);
            case RollbackTransferResult.NotAvailable:
            default:
                return await Send.NotFoundAsync(ct);
        }
    }
}
