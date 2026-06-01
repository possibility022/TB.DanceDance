using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record GetAllGroupsQuery : IRequest<IReadOnlyCollection<GroupDto>>
{
}

public record GetGroupByIdQuery : IRequest<GroupDto?>
{
    public Guid Id { get; init; }
}