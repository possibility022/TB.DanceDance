namespace TB.DanceDance.Videos.Domain.Entities;
public class Video
{
    private Video() { }
    
    public Guid Id { get; init; }
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

    /// <summary>
    /// Size of the source blob in bytes. 0 if not calculated yet.
    /// </summary>
    public long SourceBlobSize { get; set; } = 0;

    /// <summary>
    /// Size of the converted blob in bytes. 0 if not calculated yet.
    /// Used for storage quota enforcement for private videos.
    /// </summary>
    public long ConvertedBlobSize { get; set; } = 0;

    /// <summary>
    /// Controls who can see comments on this video.
    /// </summary>
    public CommentVisibility CommentVisibility { get; set; } = CommentVisibility.OwnerOnly;

    public ICollection<SharedWith> SharedWith { get; set; } = null!;

    public class Factory
    {
        /// <summary>
        /// Creates a brand-new, not-yet-converted video for the user upload flow,
        /// with its initial set of <see cref="SharedWith"/> entries.
        /// </summary>
        public static Video CreateForUpload(
            string name,
            string fileName,
            string sourceBlobId,
            string userId,
            ICollection<SharedWith> sharedWith)
        {
            return new Video()
            {
                Id = Guid.NewGuid(),
                FileName = fileName,
                SourceBlobId = sourceBlobId,
                Name = name,
                UploadedBy = userId,
                Duration = null,
                RecordedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                SharedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                SharedWith = sharedWith,
                Converted = false
            };
        }
    }
}
