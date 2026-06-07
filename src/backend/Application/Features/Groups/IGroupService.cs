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
    Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, Guid groupId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
