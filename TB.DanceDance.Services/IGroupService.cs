using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;
public interface IGroupService
{
    IQueryable<VideoInfo> GetUserVideosFromGroups(string userId);
}
