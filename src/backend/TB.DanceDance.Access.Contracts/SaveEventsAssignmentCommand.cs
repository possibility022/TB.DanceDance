using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record SaveEventsAssignmentCommand : IRequest<bool>
{
    public required string UserId { get; init; }
    public required ICollection<Guid> Events { get; init; }
}
