namespace Domain.Entities;
public class Video
{
    public Guid Id { get; set; }
    public string? BlobId { get; set; }
    public string Name { get; set; }

    // User Id
    public required string UploadedBy { get; init; }
    public required DateTime RecordedDateTime { get; set; }
    public required DateTime SharedDateTime { get; init; }
    public required TimeSpan? Duration { get; set; }
    public required string FileName { get; init; }

    /// <summary>
    /// When value is set, it means that it is being converted by some service and if we passed that date it means somethin went wrong and another service can try convert it again.
    /// </summary>
    public DateTime? LockedTill { get; set; } = null;

    /// <summary>
    /// Original video
    /// </summary>
    public required string SourceBlobId { get; init; }

    public bool Converted { get; set; } = false;

    public ICollection<SharedWith> SharedWith { get; set; } = null!;
}
