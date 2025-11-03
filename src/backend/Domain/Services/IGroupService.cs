using Domain.Entities;

namespace Domain.Services;
public interface IGroupService
{
    Task<ICollection<Group>> GetAllGroups(CancellationToken token);
    IQueryable<VideoFromGroupInfo> GetUserVideosForGroup(string userId, Guid groupId);
    IQueryable<VideoFromGroupInfo> GetUserVideosForAllGroups(string userId);
}
