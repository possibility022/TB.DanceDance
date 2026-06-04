namespace Application.Features.Videos.Models;

public record UploadUrlResponse
{
    public string Sas { get; set; }
    public Guid VideoId { get; set; }
    public DateTimeOffset ExpireAt { get; set; }
}