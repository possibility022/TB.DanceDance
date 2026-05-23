namespace TB.DanceDance.Access.Domain.Entities;

public class AssignedToEvent
{
    private AssignedToEvent() { } //for EF
    
    public Guid Id { get; set; }

    public required Guid EventId { get; init; }

    public required string UserId { get; init; }

    public Event Event { get; set; } = null!;
    
    public User User { get; set; } = null!;
}
