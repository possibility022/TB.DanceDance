namespace Domain.Entities;

public class AssignedToGroup
{
    public Guid Id { get; set; }

    public required Guid GroupId { get; init; }

    public required string UserId { get; init; }

    public required DateTime WhenJoined { get; set; }

    public Group Group { get; set; } = null!;
    public User User { get; set; } = null!;
}
