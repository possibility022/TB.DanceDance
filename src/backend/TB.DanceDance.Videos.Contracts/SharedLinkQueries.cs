using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Creates a public short-link for sharing a video. The requesting user must be the video
/// owner or have access to it. Throws <see cref="ArgumentException"/> when the expiration is
/// out of the 1–365 range, the video is not found, or the user is not authorized.
/// </summary>
public record CreateSharedLinkCommand : IRequest<SharedLinkDto>
{
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public required int ExpirationDays { get; init; }
    public required bool AllowComments { get; init; }
    public required bool AllowAnonymousComments { get; init; }
}

/// <summary>
/// Gets the shared video by its link id. Returns null if the link does not exist, is expired,
/// or has been revoked.
/// </summary>
public record GetVideoBySharedLinkQuery(string LinkId) : IRequest<VideoDto?>;

/// <summary>
/// Revokes a shared link. Allowed for the link creator or the video owner. Returns false if the
/// link is not found or the user is not authorized.
/// </summary>
public record RevokeSharedLinkCommand(string LinkId, string UserId) : IRequest<bool>;

/// <summary>
/// Gets all shared links created by the user or for videos owned by the user (newest first).
/// </summary>
public record GetUserSharedLinksQuery(string UserId) : IRequest<IReadOnlyCollection<SharedLinkDto>>;

/// <summary>
/// Gets a shared link by its id. Returns null if it does not exist, is expired, or is revoked.
/// </summary>
public record GetSharedLinkQuery(string LinkId) : IRequest<SharedLinkDto?>;
