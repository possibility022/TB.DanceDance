namespace Domain.Entities;

/// <summary>
/// A single-use invitation to join one group or one event, shared via a short link. Mirrors
/// <see cref="SharedLink"/> / <see cref="VideoTransfer"/> (short id, creator, expiry, status) but
/// adds the single-redemption race-safety guarantee neither of those needs.
/// </summary>
public class InviteLink
{
    /// <summary>Fixed, system-wide expiration window in days (not creator-configurable).</summary>
    public const int ExpirationDays = 7;

    /// <summary>Short, URL-safe link id (see <see cref="ShortLinkGenerator"/>).</summary>
    public string Id { get; set; } = null!;

    /// <summary>The group this link targets, if any. Exactly one of <see cref="GroupId"/> / <see cref="EventId"/> is set.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>The event this link targets, if any. Exactly one of <see cref="GroupId"/> / <see cref="EventId"/> is set.</summary>
    public Guid? EventId { get; set; }

    /// <summary>User id of the admin who generated the link.</summary>
    public string CreatedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }

    public InviteLinkStatus Status { get; set; } = InviteLinkStatus.Active;

    /// <summary>User id who successfully redeemed it; null until redemption.</summary>
    public string? RedeemedByUserId { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }

    public Group? Group { get; set; }
    public Event? Event { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public User? RedeemedByUser { get; set; }
}
