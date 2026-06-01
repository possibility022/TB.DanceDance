using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record AddOrUpdateUserCommand : IRequest<bool>
{
    public required string Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
}
