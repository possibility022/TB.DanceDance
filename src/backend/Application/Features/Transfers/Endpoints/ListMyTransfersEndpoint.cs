using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Lists the current user's outgoing transfers, newest first. Requires authentication.
/// </summary>
public class ListMyTransfersEndpoint : EndpointWithoutRequest<ListMyTransfersResponse>
{
    private readonly ITransferService transferService;
    private readonly IOptions<AppOptions> appOptions;

    public ListMyTransfersEndpoint(ITransferService transferService, IOptions<AppOptions> appOptions)
    {
        this.transferService = transferService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Transfer.ListMy);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var transfers = await transferService.ListMyOutgoingTransfersAsync(userId, ct);
        var origin = appOptions.Value.AppWebsiteOrigin;

        var response = new ListMyTransfersResponse
        {
            Transfers = transfers
                .Select(t => TransferMapper.MapToSummary(t, origin))
                .ToArray()
        };

        await Send.OkAsync(response, ct);
    }
}
