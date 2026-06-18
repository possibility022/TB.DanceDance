using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Declines a pending transfer (recipient action). Requires authentication.
/// </summary>
public class DeclineTransferEndpoint : EndpointWithoutRequest
{
    private readonly ITransferService transferService;

    public DeclineTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Decline);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        var result = await transferService.DeclineTransferAsync(linkId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
