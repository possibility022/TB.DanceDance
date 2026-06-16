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

public interface ITransferService
{
    /// <summary>
    /// Creates a pending transfer of a single video from the sender to whoever accepts the link.
    /// The video must be owned by the sender, converted, private, and not already in an active
    /// pending transfer.
    /// </summary>
    /// <exception cref="ArgumentException">Invalid input, ineligible video, or the video is already in a pending transfer.</exception>
    Task<VideoTransfer> CreateTransferAsync(string userId, Guid videoId, int expirationDays, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a transfer (with its items and videos) by link id. Returns null if the link doesn't
    /// exist, is expired, was revoked, or was declined.
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
    /// </summary>
    /// <exception cref="QuotaExceededException">Accepting would exceed the recipient's storage quota.</exception>
    Task<AcceptTransferResult> AcceptTransferAsync(string linkId, string userId, CancellationToken cancellationToken);
}
