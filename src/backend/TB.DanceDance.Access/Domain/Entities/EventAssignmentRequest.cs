namespace TB.DanceDance.Access.Domain.Entities;

public class EventAssignmentRequest : AssigmentRequestBase
{
    private EventAssignmentRequest() { }
    
    public Guid Id { get; set; }
    
    public required Guid EventId { get; init; }
    
    public class Factory
    {
        public static EventAssignmentRequest Create(string userId, Guid eventId)
        {
            return new EventAssignmentRequest
            {
                UserId = userId,
                EventId = eventId,
            };
        }
    }

}
