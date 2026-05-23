namespace TB.DanceDance.Access.Domain.Entities;

public class GroupAssignmentRequest : AssigmentRequestBase
{
    private GroupAssignmentRequest() { }
    
    public Guid Id { get; set; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
}
