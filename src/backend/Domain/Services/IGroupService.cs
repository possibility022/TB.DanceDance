using Domain.Entities;

namespace Domain.Services;
public interface IGroupService
{
    IQueryable<VideoFromGroupInfo> GetUserVideosForGroup(string userId, Guid groupId);
    IQueryable<VideoFromGroupInfo> GetUserVideosForAllGroups(string userId);
}
