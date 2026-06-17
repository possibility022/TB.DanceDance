namespace Domain.Entities;

/// <summary>
/// Lifecycle of a <see cref="VideoTransfer"/>.
/// </summary>
public enum TransferStatus
{
    /// <summary>Awaiting the recipient's decision. The sender still owns the videos.</summary>
    Pending = 0,

    /// <summary>The recipient accepted; awaiting the original owner's second approval. Ownership has NOT moved yet.</summary>
    Accepted = 1,

    /// <summary>The recipient declined the transfer.</summary>
    Declined = 2,

    /// <summary>The sender revoked the transfer before it was accepted.</summary>
    Revoked = 3,

    /// <summary>The owner approved after the recipient accepted; ownership has moved to the recipient.</summary>
    Approved = 4,

    /// <summary>The owner cancelled after the recipient accepted; ownership is unchanged.</summary>
    Cancelled = 5,
}
