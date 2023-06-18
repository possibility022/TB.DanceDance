using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services.Models;

public class VideoInfo
{
    public Video Video { get; set; }
    public bool SharedWithEvent { get; set; }
    public bool SharedWithGroup { get; set;}
}
