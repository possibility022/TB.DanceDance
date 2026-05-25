using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record SaveGroupsAssignmentCommand : IRequest<bool>
{
    public required string UserId { get; init; }
    public required ICollection<(Guid GroupId, DateTime JoinedDate)> Groups { get; init; }
}
