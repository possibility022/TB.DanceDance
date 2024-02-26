namespace Domain.Entities;

public class EventAssigmentRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required Guid EventId { get; init; }
}
