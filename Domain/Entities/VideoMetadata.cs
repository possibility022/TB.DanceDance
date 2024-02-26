namespace Domain.Entities;

public class VideoMetadata
{
    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public byte[] Metadata { get; set; } = null!;
}
