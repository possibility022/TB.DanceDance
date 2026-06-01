using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record GetAccessRequestsToApproveQuery : IRequest<IReadOnlyCollection<RequestedAccess>>
{
    public required string UserId { get; init; }
}
