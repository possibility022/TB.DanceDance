using Domain.Entities;
using Domain.Exceptions;

namespace Application.Features.Transfers;

/// <summary>
/// Outcome of an attempt to accept a transfer, for cases that aren't a hard error.
/// </summary>
public enum AcceptTransferResult
{
    Accepted,
    /// <summary>Link not found, expired, or no longer pending.</summary>
    NotAvailable,
    /// <summary>The accepting user is the sender — can't transfer to yourself.</summary>
    CannotAcceptOwnTransfer,
}

/// <summary>
/// Outcome of an attempt to roll back a transfer.
/// </summary>
public enum RollbackTransferResult
{
    RolledBack,
    /// <summary>Transfer not found or not in Accepted state.</summary>
    NotAvailable,
    /// <summary>The user attempting the rollback is not the original sender.</summary>
    NotOwner,
    /// <summary>The rollback window has closed.</summary>
    WindowExpired,
}

public interface ITransferService
{
    /// <summary>
    /// Creates a pending transfer of a single video from the sender to whoever accepts the link.
    /// The video must be owned by the sender, converted, private, and not already in an active
    /// pending transfer or still within another transfer's rollback window.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid input, ineligible video, already in a pending
    /// transfer, or still within a prior transfer's rollback window.</exception>
    Task<VideoTransfer> CreateTransferAsync(string userId, Guid videoId, int expirationDays, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a transfer (with its items and videos) by link id. Returns null if the link doesn't
    /// exist, was revoked, was declined, was rolled back, or is a Pending link past its expiry.
    /// Accepted transfers are always returned regardless of expiry.
    /// </summary>
    Task<VideoTransfer?> GetTransferAsync(string linkId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the outgoing transfers created by the user, newest first (with items + videos).
    /// </summary>
    Task<IReadOnlyCollection<VideoTransfer>> ListMyOutgoingTransfersAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a pending transfer. Sender-only. Returns false if not found or the user isn't the sender.
    /// </summary>
    Task<bool> RevokeTransferAsync(string linkId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Declines a pending transfer. The decliner must not be the sender. Returns false if not found,
    /// not pending, or the user is the sender.
    /// </summary>
    Task<bool> DeclineTransferAsync(string linkId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Accepts a pending transfer: moves ownership of every item to the recipient atomically,
    /// re-points the private share rows, revokes the sender's active share links for those videos,
    /// and marks the transfer accepted. Blocked if it would exceed the recipient's storage quota.
    /// The sender can still roll this back for <see cref="VideoTransfer.RollbackWindowDays"/> days.
    /// </summary>
    /// <exception cref="QuotaExceededException">Accepting would exceed the recipient's storage quota.</exception>
    Task<AcceptTransferResult> AcceptTransferAsync(string linkId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Sender rolls back an Accepted transfer within the rollback window: ownership and the private
    /// share rows move back to the sender. The sender's share links that were revoked at acceptance
    /// time stay revoked.
    /// </summary>
    Task<RollbackTransferResult> RollbackTransferAsync(string linkId, string ownerUserId, CancellationToken cancellationToken);
}
