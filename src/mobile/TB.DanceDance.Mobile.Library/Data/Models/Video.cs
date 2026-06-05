using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;

namespace TB.DanceDance.Mobile.Library.Data.Models;

public record UploadState
{
    public bool Uploaded { get; set; }
}

public record Video
{
    public Guid Id { get; set; }
    public string BlobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime When { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public UploadState? UploadState { get; set; }
    public bool Converted { get; set; } = false;


    public static List<Video> MapFromApiResponse(IReadOnlyCollection<VideoFromGroupInformation>? videosResponse)
    {
        if (videosResponse == null)
            return new List<Video>();

        return videosResponse.Select(v => new Video()
        {
            Id = v.VideoId,
            Name = v.Name,
            GroupName = v.GroupName,
            When = v.RecordedDateTime,
            GroupId = v.GroupId,
            BlobId = v.BlobId,
            Converted = v.Converted,
        }).ToList();
    }

    public static List<Video> MapFromApiResponse(IReadOnlyCollection<VideoInformation> videosForEvent)
    {
        return videosForEvent.Select(r => new Video()
        {
            Id = r.VideoId,
            Name = r.Name,
            When = r.RecordedDateTime,
            BlobId = r.BlobId,
            Converted = r.Converted,
        }).ToList();
    }
}
