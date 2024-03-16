namespace Domain.Entities;

public class EventAssigmentRequest : AssigmentRequestBase
{
    public Guid Id { get; set; }
    
    public required Guid EventId { get; init; }

}
