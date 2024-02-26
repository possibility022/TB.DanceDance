namespace Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required DateTime Date { get; init; }
    public required EventType Type { get; init; }

    public ICollection<SharedWith> HasSharedVideos { get; set; } = null!;
}
