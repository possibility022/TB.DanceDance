using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Gets transfer info by link id for the landing page. Requires authentication (NOT anonymous,
/// unlike the shared-link info endpoint).
/// </summary>
public class GetTransferInfoEndpoint : EndpointWithoutRequest<TransferInfoResponse>
{
    private readonly ITransferService transferService;

    public GetTransferInfoEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Transfer.GetInfo);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var linkId = Route<string>("linkId") ?? string.Empty;
        var transfer = await transferService.GetTransferAsync(linkId, ct);

        if (transfer == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = TransferMapper.MapToInfo(transfer);
        await Send.OkAsync(response, ct);
    }
}
