namespace Domain.Entities;

/// <summary>
/// Lifecycle of a <see cref="VideoTransfer"/>.
/// </summary>
public enum TransferStatus
{
    /// <summary>Awaiting the recipient's decision. The sender still owns the videos.</summary>
    Pending = 0,

    /// <summary>The recipient accepted; ownership of the items has moved to them. The sender can
    /// still roll this back for <see cref="VideoTransfer.RollbackWindowDays"/> days.</summary>
    Accepted = 1,

    /// <summary>The recipient declined the transfer.</summary>
    Declined = 2,

    /// <summary>The sender revoked the transfer before it was accepted.</summary>
    Revoked = 3,

    /// <summary>The sender reversed an Accepted transfer within the rollback window; ownership
    /// moved back to the sender.</summary>
    RolledBack = 4,
}
