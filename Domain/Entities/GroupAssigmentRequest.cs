namespace Domain.Entities;

public class GroupAssigmentRequest
{
    public Guid Id { get; set; }
    public required string UserId { get; init; }
    public required string UserDisplayName { get; init; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
}
