using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

/// <summary>
/// Resolves display details for a set of users by id. Consumed by the Videos/Comments edge to
/// turn a comment's <c>UserId</c> into an author display name without navigating to the Access
/// <c>User</c> entity cross-module.
/// </summary>
public record GetUsersByIdsQuery(IReadOnlyCollection<string> UserIds)
    : IRequest<IReadOnlyCollection<UserInfoDto>>;

public record UserInfoDto
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}
