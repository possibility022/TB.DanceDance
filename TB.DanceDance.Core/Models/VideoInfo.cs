using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services.Models;

/// <summary>
/// Represents a video that is shared with given group
/// </summary>
public class VideoFromGroupInfo
{
    public Video Video { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; }
}
