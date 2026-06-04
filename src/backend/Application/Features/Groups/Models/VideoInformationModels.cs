namespace Application.Features.Groups.Models;

public record VideoFromGroupInformation : VideoInformation
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; }
}

public record VideoInformation
{
    public Guid VideoId { get; set; }
    public string BlobId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime RecordedDateTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool Converted { get; set; }
    public int CommentVisibility { get; set; }
}

