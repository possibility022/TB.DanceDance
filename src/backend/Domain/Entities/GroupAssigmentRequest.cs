namespace Domain.Entities;

public class GroupAssigmentRequest : AssigmentRequestBase
{
    public Guid Id { get; set; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
}
