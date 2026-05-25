namespace TB.DanceDance.Videos.Domain.Entities;

public class VideoMetadata
{
    private VideoMetadata() { }

    public Guid Id { get; set; }
    public Guid VideoId { get; set; }
    public byte[] Metadata { get; set; } = null!;

    public static class Factory
    {
        public static VideoMetadata Create(Guid videoId, byte[] metadata)
            => new() { VideoId = videoId, Metadata = metadata };
    }
}
