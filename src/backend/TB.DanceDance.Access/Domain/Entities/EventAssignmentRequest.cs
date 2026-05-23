namespace TB.DanceDance.Access.Domain.Entities;

public class EventAssignmentRequest : AssigmentRequestBase
{
    private EventAssignmentRequest() { }
    
    public Guid Id { get; set; }
    
    public required Guid EventId { get; init; }

}
