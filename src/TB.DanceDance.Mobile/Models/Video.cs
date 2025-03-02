using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Mobile.Models;

public record Video
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime When { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid GroupId { get; set; }

    public static List<Video> MapFromApiEvent(ICollection<GroupWithVideosResponse>? videosResponse)
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
                    });
                }
            }
        }

        return list;
    }
}