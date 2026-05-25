using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record ApproveAccessRequestCommand : IRequest<bool>
{
    public required Guid RequestId { get; init; }
    public required bool IsGroup { get; init; }
    public required string UserId { get; init; }
}
