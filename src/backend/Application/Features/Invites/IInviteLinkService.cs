using Domain.Entities;

namespace Application.Features.Invites;

/// <summary>Public preview of an invite link, shown before the caller signs in.</summary>
public record InviteLinkInfo(string Id, string TargetType, string TargetName, bool IsRedeemable);

/// <summary>Outcome of an attempt to redeem an invite link.</summary>
public enum RedeemInviteLinkResult
{
    Redeemed,
    /// <summary>The caller already had membership/access; no state change (FR-010).</summary>
    AlreadyMember,
    /// <summary>Link not found, already redeemed by someone else, revoked, or expired (FR-005).</summary>
    NotAvailable,
}

/// <summary>Outcome of an attempt to revoke an invite link.</summary>
public enum RevokeInviteLinkResult
{
    Revoked,
    /// <summary>The link was already redeemed; revocation has no effect (User Story 4, scenario 3).</summary>
    AlreadyRedeemed,
    NotAuthorized,
    NotFound,
}

public interface IInviteLinkService
{
    /// <summary>
    /// Creates an invite link for a group. Caller must be a current admin of the group.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The caller is not a current admin of the group.</exception>
    Task<InviteLink> CreateForGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an invite link for an event. Caller must be the event's owner/admin.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The caller is not the event's owner.</exception>
    Task<InviteLink> CreateForEventAsync(Guid eventId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the public preview of an invite link (target type/name, whether it's still redeemable).
    /// Returns null if the link id doesn't exist at all.
    /// </summary>
    Task<InviteLinkInfo?> GetInfoAsync(string linkId, CancellationToken cancellationToken);

    /// <summary>
    /// Redeems an invite link for the caller. Race-safe: under concurrent attempts on the same link,
    /// exactly one ever transitions the link to Redeemed.
    /// </summary>
    Task<RedeemInviteLinkResult> RedeemAsync(string linkId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all invite links for a group, newest first. Caller must be a current admin of the group,
    /// independent of which admin created any given link (FR-008).
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The caller is not a current admin of the group.</exception>
    Task<IReadOnlyCollection<InviteLink>> ListForGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all invite links for an event, newest first. Caller must be the event's owner/admin.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">The caller is not the event's owner.</exception>
    Task<IReadOnlyCollection<InviteLink>> ListForEventAsync(Guid eventId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes an invite link. Caller must be a current admin of the link's target group/event,
    /// independent of who created the link (FR-007). A no-op when the link was already redeemed.
    /// </summary>
    Task<RevokeInviteLinkResult> RevokeAsync(string linkId, string userId, CancellationToken cancellationToken);
}
