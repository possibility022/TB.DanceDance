using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record GetPendingUserRequestsQuery : IRequest<UserRequests>
{
    public required string UserId { get; init; }
}
