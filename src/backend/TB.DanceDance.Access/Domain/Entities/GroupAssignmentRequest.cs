namespace TB.DanceDance.Access.Domain.Entities;

public class GroupAssignmentRequest : AssigmentRequestBase
{
    private GroupAssignmentRequest() { }
    
    public Guid Id { get; set; }
    public required Guid GroupId { get; init; }
    public required DateTime WhenJoined { get; set; }
    
    public class Factory
    {
        public static GroupAssignmentRequest Create(string userId, Guid groupId, DateTime whenJoined)
        {
            return new GroupAssignmentRequest
            {
                UserId = userId,
                GroupId = groupId,
                WhenJoined = whenJoined,
                Id = Guid.NewGuid(),
            };
        }
    }
}
