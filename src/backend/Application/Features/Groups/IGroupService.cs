using Application.Domain.Models;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using Group = Domain.Entities.Group;

namespace Application.Features.Groups;
public interface IGroupService
{
    Task<ICollection<Group>> GetAllGroups(CancellationToken cancellationToken);

    /// <summary>True when the user is recorded as an admin of the group.</summary>
    Task<bool> IsGroupAdmin(Guid groupId, string userId, CancellationToken cancellationToken);

    /// <summary>Ids of the groups the user administers (used to surface management entry points).</summary>
    Task<Guid[]> GetAdministeredGroupIdsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>The groups the user administers, with full group details (used for the "my groups" navigation entry point).</summary>
    Task<GroupModel[]> GetAdministeredGroupsAsync(string userId, CancellationToken cancellationToken);

    /// <summary>Creates a group and records the creator as its first admin in one transaction.</summary>
    Task<Group> CreateGroupAsync(string name, DateOnly seasonStart, DateOnly seasonEnd, string creatorUserId, CancellationToken cancellationToken);

    Task<GroupAdminModel[]> GetAdminsAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>Adds an admin (idempotent). Returns false when the target user does not exist.</summary>
    Task<bool> AddAdminAsync(Guid groupId, string userId, CancellationToken cancellationToken);

    /// <summary>Removes an admin, blocking removal of the last remaining admin.</summary>
    Task<RemoveGroupAdminResult> RemoveAdminAsync(Guid groupId, string userId, CancellationToken cancellationToken);

    Task<GroupMemberModel[]> GetMembersAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>Updates a member's join date. Returns false when the membership does not exist.</summary>
    Task<bool> UpdateMemberJoinedAsync(Guid groupId, string userId, DateTime whenJoined, CancellationToken cancellationToken);

    /// <summary>Revokes a member's access (deletes the membership). Returns false when not a member.</summary>
    Task<bool> RemoveMemberAsync(Guid groupId, string userId, CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForGroup(string userId, Guid groupId, CancellationToken cancellationToken);
    Task<VideoFromGroupInfo[]> GetUserVideosForAllGroups(string userId, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, Guid groupId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
