namespace Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    
    public required DateOnly SeasonStart { get; set; }
    public required DateOnly SeasonEnd { get; set; }

    public ICollection<SharedWith> HasSharedVideos { get; set; } = null!;
}
