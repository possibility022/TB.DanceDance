using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Creates a transfer of one or more owned, converted, private videos. Requires authentication.
/// </summary>
public class CreateTransferEndpoint : Endpoint<CreateTransferRequest, TransferSummaryResponse>
{
    private readonly ITransferService transferService;
    private readonly IOptions<AppOptions> appOptions;

    public CreateTransferEndpoint(ITransferService transferService, IOptions<AppOptions> appOptions)
    {
        this.transferService = transferService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Transfer.Create);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CreateTransferRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        try
        {
            var transfer = await transferService.CreateTransferAsync(userId, req.VideoIds, req.ExpirationDays, ct);

            // Re-fetch with items + videos so the response carries titles / sizes.
            var full = await transferService.GetTransferAsync(transfer.Id, ct) ?? transfer;
            var response = TransferMapper.MapToSummary(full, appOptions.Value.AppWebsiteOrigin);
            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to create transfer for user {UserId}", userId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
