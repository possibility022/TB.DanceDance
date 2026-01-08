using Domain.Entities;

namespace Domain.Services;

public interface ISharedLinkService
{
    /// <summary>
    /// Creates a shared link for a video. The user must be the video owner or have SharedWith access.
    /// </summary>
    /// <param name="videoId">The ID of the video to share</param>
    /// <param name="userId">The ID of the user creating the share link</param>
    /// <param name="expirationDays">Number of days until link expires (1-365, default 7)</param>
    /// <param name="allowComments">Whether commenting is allowed through this link</param>
    /// <param name="allowAnonymousComments">Whether anonymous commenting is allowed through this link</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created SharedLink</returns>
    /// <exception cref="ArgumentException">If expiration days is out of range or video not found or user unauthorized</exception>
    Task<SharedLink> CreateSharedLinkAsync(Guid videoId, string userId, int expirationDays, bool allowComments, bool allowAnonymousComments, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a video by its shared link ID. Returns null if link doesn't exist, is expired, or is revoked.
    /// </summary>
    /// <param name="linkId">The short link ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The video if link is valid and active, null otherwise</returns>
    Task<Video?> GetVideoBySharedLinkAsync(string linkId, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a shared link. User must be the link creator or the video owner.
    /// </summary>
    /// <param name="linkId">The short link ID to revoke</param>
    /// <param name="userId">The ID of the user requesting revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if revoked successfully, false if link not found or user unauthorized</returns>
    Task<bool> RevokeSharedLinkAsync(string linkId, string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all shared links created by the user or for videos owned by the user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of shared links with video details</returns>
    Task<IReadOnlyCollection<SharedLink>> GetUserSharedLinksAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a shared link by its ID with video details. Returns null if link doesn't exist, is expired, or is revoked.
    /// </summary>
    /// <param name="linkId">The short link ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The shared link if valid and active, null otherwise</returns>
    Task<SharedLink?> GetSharedLinkAsync(string linkId, CancellationToken cancellationToken);
}
