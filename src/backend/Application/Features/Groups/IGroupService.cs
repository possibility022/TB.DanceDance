using Application.Domain.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using Group = Domain.Entities.Group;

namespace Application.Features.Groups;
public interface IGroupService
{
    Task<ICollection<Group>> GetAllGroups(CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForGroup(string userId, Guid groupId, CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForAllGroups(string userId, CancellationToken cancellationToken);
    Task<VideoFromGroupInformation[]> GetAllVideos(string userId, CancellationToken cancellationToken);
    Task<VideoFromGroupInformation[]> GetAllVideos(string userId, Guid groupId, CancellationToken cancellationToken);
}
