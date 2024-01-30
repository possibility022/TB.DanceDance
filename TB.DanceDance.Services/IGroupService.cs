using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;
public interface IGroupService
{
    IQueryable<VideoFromGroupInfo> GetUserVideosFromGroups(string userId);
}
