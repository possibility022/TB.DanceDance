using TB.DanceDance.API.Contracts.Responses;

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


    public static List<Video> MapFromApiResponse(ICollection<GroupWithVideosResponse>? videosResponse)
    {
        if (videosResponse == null)
            return new List<Video>();

        var list = new List<Video>();

        foreach (var groupWithVideosResponse in videosResponse)
        {
            if (groupWithVideosResponse?.Videos != null)
            {
                foreach (var videoInformationModel in groupWithVideosResponse.Videos)
                {
                    list.Add(new Video()
                    {
                        Id = videoInformationModel.Id,
                        Name = videoInformationModel.Name,
                        GroupName = groupWithVideosResponse.GroupName,
                        When = videoInformationModel.RecordedDateTime,
                        GroupId = groupWithVideosResponse.GroupId,
                        BlobId = videoInformationModel.BlobId,
                        Converted = videoInformationModel.Converted,
                    });
                }
            }
        }

        return list;
    }

    public static List<Video> MapFromApiResponse(ICollection<VideoInformationResponse> videosForEvent)
    {
        var list = videosForEvent.Select(r => new Video()
        {
            Id = r.Id, 
            Name = r.Name, 
            When = r.RecordedDateTime, 
            BlobId = r.BlobId,
            Converted = r.Converted,
        }).ToList();

        return list;
    }
}