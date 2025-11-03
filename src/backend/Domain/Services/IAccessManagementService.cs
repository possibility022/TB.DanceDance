using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IAccessManagementService
{
    Task AddOrUpdateUserAsync(User user, CancellationToken cancellationToken);
    Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId);
    Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId, CancellationToken cancellationToken);
    Task<UserRequests> GetPendingUserRequests(string userId, CancellationToken cancellationToken);
    Task<ICollection<RequestedAccess>> GetAccessRequestsToApproveAsync(string userId,
        CancellationToken cancellationToken);
    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events, CancellationToken cancellationToken);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups,
        CancellationToken cancellationToken);
}
