namespace Domain.Entities;

public class AssignedToEvent
{
    public Guid Id { get; set; }

    public required Guid EventId { get; init; }

    public required string UserId { get; init; }

    public Event Event { get; set; } = null!;
}
