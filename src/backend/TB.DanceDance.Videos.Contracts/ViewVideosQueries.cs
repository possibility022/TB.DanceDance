using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Videos.Contracts;

/// <summary>
/// Videos shared to a single group, filtered to the requesting user's membership and the
/// "recorded after the user joined" rule. Returns empty if the user is not a member.
/// </summary>
public record ViewVideosFromGroupQuery(string UserId, Guid GroupId)
    : IRequest<IReadOnlyCollection<VideoDto>>;

/// <summary>
/// Videos shared to a single event, filtered to the requesting user's event membership.
/// Returns empty if the user is not a member.
/// </summary>
public record ViewVideosFromEventQuery(string UserId, Guid EventId)
    : IRequest<IReadOnlyCollection<VideoDto>>;

/// <summary>
/// Videos shared to any group the user belongs to, each filtered by that group's join date.
/// </summary>
public record ViewVideosFromAllGroupsQuery(string UserId)
    : IRequest<IReadOnlyCollection<VideoDto>>;

/// <summary>
/// The user's private videos: shares with no event and no group, owned by the requester.
/// </summary>
public record ViewPrivateVideosQuery(string UserId)
    : IRequest<IReadOnlyCollection<VideoDto>>;
