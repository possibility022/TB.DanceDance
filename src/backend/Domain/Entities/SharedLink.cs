namespace Domain.Entities;

public class SharedLink
{
    public string Id { get; set; } = null!;
    public Guid VideoId { get; set; }
    public string SharedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    public User SharedByUser { get; set; } = null!;
    public Video Video { get; set; } = null!;
}