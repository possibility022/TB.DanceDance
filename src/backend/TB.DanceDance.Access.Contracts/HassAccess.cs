using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Access.Contracts;

public record DoesUserHasAccessToSharedWith : IRequest<bool>
{
    public required string UserId { get; init; }
    public required Guid SharedToId { get; init; }
    public required SharedWithByType SharedWithType { get; init; }
    public DateTime? WhenJoined { get; set; } = DateTime.UtcNow;
}