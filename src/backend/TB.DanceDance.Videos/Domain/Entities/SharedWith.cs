namespace TB.DanceDance.Videos.Domain.Entities;
public class SharedWith
{
    private SharedWith() { }
    
    public Guid Id { get; set; }
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public Guid? EventId { get; set; }
    public Guid? GroupId { get; set; }

    public Video Video { get; set; } = null!;

    public class Factory
    {
        public static SharedWith Create(string userId, Guid? eventId, Guid? groupId)
        {
            return new SharedWith()
            {
                VideoId = Guid.Empty, // should be set by EF
                UserId = userId,
                EventId = eventId,
                GroupId = groupId
            };
        }
    }
}