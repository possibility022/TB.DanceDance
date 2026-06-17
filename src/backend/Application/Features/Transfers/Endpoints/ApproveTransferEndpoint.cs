using Application.Extensions;
using Domain.Exceptions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Owner's second approval after the recipient accepted. Moves ownership to the recipient
/// and marks the transfer Approved. Requires authentication (sender only).
/// A quota overrun at approval time returns 409 with the required / available byte counts.
/// </summary>
public class ApproveTransferEndpoint : EndpointWithoutRequest<AcceptTransferResponse>
{
    private readonly ITransferService transferService;

    public ApproveTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Approve);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        try
        {
            var result = await transferService.ApproveTransferAsync(linkId, userId, ct);

            switch (result)
            {
                case ApproveTransferResult.Approved:
                    await Send.OkAsync(new AcceptTransferResponse { Accepted = true }, ct);
                    return;
                case ApproveTransferResult.NotOwner:
                    await Send.ForbiddenAsync(ct);
                    return;
                case ApproveTransferResult.NotAvailable:
                default:
                    await Send.NotFoundAsync(ct);
                    return;
            }
        }
        catch (QuotaExceededException ex)
        {
            await Send.ResponseAsync(new AcceptTransferResponse
            {
                Accepted = false,
                RequiredBytes = ex.RequiredBytes,
                AvailableBytes = ex.AvailableBytes,
                Error = ex.Message
            }, 409, ct);
        }
    }
}
