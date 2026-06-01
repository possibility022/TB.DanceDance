using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

/// <summary>
/// Returns the requesting user's group memberships together with the date they joined each
/// group. Consumed by the Videos module to apply the "members only see videos recorded after
/// they joined" rule without joining the Access <c>AssignedToGroup</c> table cross-module.
/// </summary>
public record GetUserGroupMembershipsQuery(string UserId)
    : IRequest<IReadOnlyCollection<GroupMembershipDto>>;

public record GroupMembershipDto
{
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; init; }
}
