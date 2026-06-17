using System;
using System.Collections.Generic;

namespace TB.DanceDance.API.Contracts.Features.Transfers
{
    /// <summary>One video inside a transfer, with the metadata needed to preview it.</summary>
    public class TransferItemInfo
    {
        public Guid VideoId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime RecordedDateTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public long SizeBytes { get; set; }
    }

    /// <summary>
    /// A transfer as seen by its creator (create + My Transfers list). Includes the share URL.
    /// </summary>
    public class TransferSummaryResponse
    {
        public string LinkId { get; set; } = string.Empty;
        /// <summary>Pending / Accepted / Approved / Declined / Revoked / Cancelled / Expired.</summary>
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpireAt { get; set; }
        public string ShareUrl { get; set; } = string.Empty;
        public long TotalSizeBytes { get; set; }
        public string? AcceptedByUserId { get; set; }
        public DateTimeOffset? AcceptedAt { get; set; }
        public DateTimeOffset? ApprovedAt { get; set; }
        public IReadOnlyCollection<TransferItemInfo> Items { get; set; } = Array.Empty<TransferItemInfo>();
    }

    /// <summary>
    /// A transfer as seen by the recipient on the landing page (no share URL).
    /// </summary>
    public class TransferInfoResponse
    {
        public string LinkId { get; set; } = string.Empty;
        /// <summary>Pending / Accepted / Declined / Revoked / Expired.</summary>
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset ExpireAt { get; set; }
        public long TotalSizeBytes { get; set; }
        public IReadOnlyCollection<TransferItemInfo> Items { get; set; } = Array.Empty<TransferItemInfo>();
    }

    public class ListMyTransfersResponse
    {
        public IReadOnlyCollection<TransferSummaryResponse> Transfers { get; set; } = Array.Empty<TransferSummaryResponse>();
    }

    /// <summary>
    /// Result of an accept attempt. On a quota block the request returns 409 with this body and the
    /// required / available byte counts populated.
    /// </summary>
    public class AcceptTransferResponse
    {
        public bool Accepted { get; set; }
        public long? RequiredBytes { get; set; }
        public long? AvailableBytes { get; set; }
        public string? Error { get; set; }
    }
}
