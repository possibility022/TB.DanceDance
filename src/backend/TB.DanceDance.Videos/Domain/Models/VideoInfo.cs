using TB.DanceDance.Videos.Domain.Entities;

namespace TB.DanceDance.Videos.Domain.Models;

/// <summary>
/// Represents a video that is shared with given group
/// </summary>
public class VideoFromGroupInfo
{
    public Video Video { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; }
    //public Group Group { get; set; }
}
