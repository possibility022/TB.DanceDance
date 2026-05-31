namespace TB.DanceDance.Videos.Contracts;

public record UploadContext
{
    public required Guid VideoId { get; init; }
    public required string SourceBlobId { get; init; }
    public required Uri Sas { get; init; }
    public required DateTimeOffset ExpireAt { get; set; }
}