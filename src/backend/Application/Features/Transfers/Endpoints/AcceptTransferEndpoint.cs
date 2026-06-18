using Application.Extensions;
using Domain.Exceptions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Accepts a pending transfer, moving ownership to the current user. Requires authentication.
/// A quota overrun returns 409 with the required / available byte counts.
/// </summary>
public class AcceptTransferEndpoint : EndpointWithoutRequest<AcceptTransferResponse>
{
    private readonly ITransferService transferService;

    public AcceptTransferEndpoint(ITransferService transferService)
    {
        this.transferService = transferService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Accept);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;

        try
        {
            var result = await transferService.AcceptTransferAsync(linkId, userId, ct);

            switch (result)
            {
                case AcceptTransferResult.Accepted:
                    await Send.OkAsync(new AcceptTransferResponse { Accepted = true }, ct);
                    return;
                case AcceptTransferResult.CannotAcceptOwnTransfer:
                    AddError("You cannot accept your own transfer.");
                    await Send.ErrorsAsync(400, ct);
                    return;
                case AcceptTransferResult.NotAvailable:
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
