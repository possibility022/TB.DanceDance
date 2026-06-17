using Domain.Entities;
using TB.DanceDance.API.Contracts.Features.Transfers;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Projects <see cref="VideoTransfer"/> entities to their API contracts. Mirrors the Sharing
/// feature's <c>ShareMapper</c>.
/// </summary>
internal static class TransferMapper
{
    /// <summary>Builds the public, front-end transfer URL for a link id.</summary>
    public static string ResolveTransferUrl(string appWebsiteOrigin, string linkId)
        => $"{appWebsiteOrigin}/transfer/{linkId}";

    /// <summary>
    /// A pending transfer past its expiry shows as "Expired"; otherwise the stored status name.
    /// </summary>
    public static string ResolveStatus(VideoTransfer transfer)
    {
        if (transfer.Status == TransferStatus.Pending && transfer.ExpireAt <= DateTimeOffset.UtcNow)
            return "Expired";
        return transfer.Status.ToString();
    }

    public static TransferItemInfo MapItem(VideoTransferItem item)
    {
        var video = item.Video;
        return new TransferItemInfo
        {
            VideoId = video.Id,
            Name = video.Name,
            RecordedDateTime = video.RecordedDateTime,
            Duration = video.Duration,
            SizeBytes = video.ConvertedBlobSize
        };
    }

    public static TransferSummaryResponse MapToSummary(VideoTransfer transfer, string appWebsiteOrigin)
    {
        var items = transfer.Items.Select(MapItem).ToArray();
        return new TransferSummaryResponse
        {
            LinkId = transfer.Id,
            Status = ResolveStatus(transfer),
            CreatedAt = transfer.CreatedAt,
            ExpireAt = transfer.ExpireAt,
            ShareUrl = ResolveTransferUrl(appWebsiteOrigin, transfer.Id),
            TotalSizeBytes = items.Sum(i => i.SizeBytes),
            AcceptedByUserId = transfer.AcceptedByUserId,
            AcceptedAt = transfer.AcceptedAt,
            ApprovedAt = transfer.ApprovedAt,
            Items = items
        };
    }

    public static TransferInfoResponse MapToInfo(VideoTransfer transfer)
    {
        var items = transfer.Items.Select(MapItem).ToArray();
        return new TransferInfoResponse
        {
            LinkId = transfer.Id,
            Status = ResolveStatus(transfer),
            ExpireAt = transfer.ExpireAt,
            TotalSizeBytes = items.Sum(i => i.SizeBytes),
            Items = items
        };
    }
}
