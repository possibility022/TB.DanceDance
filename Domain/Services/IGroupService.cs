using Domain.Entities;

namespace Domain.Services;
public interface IGroupService
{
    IQueryable<VideoFromGroupInfo> GetUserVideosForGroup(string userId, Guid groupId);
    IQueryable<VideoFromGroupInfo> GetUserVideosFromGroups(string userId);
}
