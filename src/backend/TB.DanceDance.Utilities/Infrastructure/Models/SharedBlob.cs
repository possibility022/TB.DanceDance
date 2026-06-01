namespace TB.DanceDance.Utilities.Infrastructure.Models;

public class SharedBlob
{
    public required Uri Sas { get; init; }
    public required string BlobId { get; init; }
    public required DateTimeOffset ExpiresAt { get; set; }
}
