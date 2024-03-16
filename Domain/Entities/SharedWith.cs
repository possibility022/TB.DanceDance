namespace Domain.Entities;
public class SharedWith
{
    public Guid Id { get; set; }
    public required Guid VideoId { get; init; }
    public required string UserId { get; init; }
    public Guid? EventId { get; set; }
    public Guid? GroupId { get; set; }

    public Video Video { get; set; } = null!;
    public Event? Event { get; set; }
    public Group? Group { get; set; }

    public User User { get; set; } = null!;
}