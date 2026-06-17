namespace Domain.Entities;

/// <summary>
/// A batch transfer of ownership of one or more videos from the creator to a recipient,
/// shared via a short link. Mirrors <see cref="SharedLink"/> but moves ownership rather
/// than granting view access.
/// </summary>
public class VideoTransfer
{
    /// <summary>Short, URL-safe link id (see <see cref="ShortLinkGenerator"/>).</summary>
    public string Id { get; set; } = null!;

    /// <summary>User id of the sender who created the transfer.</summary>
    public string CreatedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>User id of the recipient who accepted; null until accepted.</summary>
    public string? AcceptedByUserId { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    /// <summary>When the owner approved after recipient acceptance; null until approved.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ICollection<VideoTransferItem> Items { get; set; } = null!;
}
