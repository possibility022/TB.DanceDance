using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Contracts;

public record GetUserGroupsAndEvents : IRequest<UserGroupsAndEvents>
{
    public required string UserId { get; init; }
}

public record UserGroupsAndEvents
{
    public required IReadOnlyCollection<GroupDto> Groups { get; init; }
    public required IReadOnlyCollection<EventDto> Events { get; init; }
}

public record GroupDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    
    public required DateOnly SeasonStart { get; init; }
    public required DateOnly SeasonEnd { get; init; }
}

public record AssignedGroupDto : GroupDto
{
    public DateTime WhenJoined { get; set; }
}

public record EventDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime Date { get; init; }
    public required string Owner { get; init; }
}