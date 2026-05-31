using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Videos.ViewVideo;

/// <summary>
/// Two-step, cross-module access decision that lives in the Videos module:
/// 1. Resolve the video's <c>SharedWith</c> rows locally (by blob id or video id).
/// 2. Private shares (no event, no group) are decided here by owner match.
///    Group/event shares are delegated to the Access module via
///    <see cref="DoesUserHasAccessToSharedWith"/>.
/// The user has access if ANY share grants it.
/// </summary>
class DoesUserHaveAccessToVideoHandler
    : IRequestHandler<DoesUserHaveAccessToVideoQuery, bool>,
      IRequestHandler<DoesUserHaveAccessToVideoByBlobQuery, bool>
{
    private readonly IRequestHandler<SharedWithByVideoIdQuery, IReadOnlyCollection<SharedWithResponse>> sharedWithByVideoIdQueryHandler;
    private readonly IRequestHandler<SharedWithByVideoBlobIdQuery, IReadOnlyCollection<SharedWithResponse>> sharedWithByVideoBlobIdQueryHandler;
    private readonly IRequestHandler<DoesUserHasAccessToSharedWith, bool> doesUserHasAccessToSharedWith;

    public DoesUserHaveAccessToVideoHandler(
        IRequestHandler<SharedWithByVideoIdQuery, IReadOnlyCollection<SharedWithResponse>> sharedWithByVideoIdQueryHandler,
        IRequestHandler<SharedWithByVideoBlobIdQuery, IReadOnlyCollection<SharedWithResponse>> sharedWithByVideoBlobIdQueryHandler,
        IRequestHandler<DoesUserHasAccessToSharedWith, bool> doesUserHasAccessToSharedWith)
    {
        this.sharedWithByVideoIdQueryHandler = sharedWithByVideoIdQueryHandler;
        this.sharedWithByVideoBlobIdQueryHandler = sharedWithByVideoBlobIdQueryHandler;
        this.doesUserHasAccessToSharedWith = doesUserHasAccessToSharedWith;
    }

    public async Task<bool> HandleAsync(DoesUserHaveAccessToVideoQuery request, CancellationToken cancellationToken = default)
    {
        var shares = await sharedWithByVideoIdQueryHandler.HandleAsync(new SharedWithByVideoIdQuery { VideoId = request.VideoId }, cancellationToken);
        return await HasAccessAsync(request.UserId, shares, cancellationToken);
    }

    public async Task<bool> HandleAsync(DoesUserHaveAccessToVideoByBlobQuery request, CancellationToken cancellationToken = default)
    {
        var shares = await sharedWithByVideoBlobIdQueryHandler.HandleAsync(new SharedWithByVideoBlobIdQuery { VideoBlobId = request.BlobId }, cancellationToken);
        return await HasAccessAsync(request.UserId, shares, cancellationToken);
    }

    private async Task<bool> HasAccessAsync(string userId, IReadOnlyCollection<SharedWithResponse> shares, CancellationToken cancellationToken)
    {
        foreach (var share in shares)
        {
            // Private share: no event and no group — granted only to the owner. Decided locally.
            if (share.EventId is null && share.GroupId is null)
            {
                if (share.UserId == userId)
                    return true;

                continue;
            }

            if (share.EventId is { } eventId)
            {
                var hasEventAccess = await doesUserHasAccessToSharedWith.HandleAsync(new DoesUserHasAccessToSharedWith
                {
                    UserId = userId,
                    SharedToId = eventId,
                    SharedWithType = SharedWithByType.Event,
                }, cancellationToken);

                if (hasEventAccess)
                    return true;
            }

            if (share.GroupId is { } groupId)
            {
                // The original GetBaseVideosForUserQuery granted group access purely on group
                // membership with no time restriction. SharedWith carries no share timestamp, so
                // we pass DateTime.MinValue (UTC) to make the handler's
                // AssignedToGroup.WhenJoined >= WhenJoined check always pass — replicating the
                // old behavior exactly.
                var hasGroupAccess = await doesUserHasAccessToSharedWith.HandleAsync(new DoesUserHasAccessToSharedWith
                {
                    UserId = userId,
                    SharedToId = groupId,
                    SharedWithType = SharedWithByType.Group,
                    WhenJoined = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
                }, cancellationToken);

                if (hasGroupAccess)
                    return true;
            }
        }

        return false;
    }
}
