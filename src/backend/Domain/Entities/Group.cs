namespace Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public ICollection<SharedWith> HasSharedVideos { get; set; } = null!;
}
