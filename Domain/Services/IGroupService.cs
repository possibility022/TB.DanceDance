using Domain.Entities;

namespace Domain.Services;
public interface IGroupService
{
    IQueryable<VideoFromGroupInfo> GetUserVideosFromGroups(string userId);
}
