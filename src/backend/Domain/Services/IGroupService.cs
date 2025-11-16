using Domain.Entities;

namespace Domain.Services;
public interface IGroupService
{
    Task<ICollection<Group>> GetAllGroups(CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForGroup(string userId, Guid groupId, CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForAllGroups(string userId, CancellationToken cancellationToken);
}
