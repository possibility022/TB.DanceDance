using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IAccessManagementService
{
    Task AddOrUpdateUserAsync(User user);
    Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId);
    Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId);
    Task<UserRequests> GetPendingUserRequests(string userId, CancellationToken cancellationToken);
    Task<ICollection<RequestedAccess>> GetAccessRequestsToApproveAsync(string userId);
    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups);
}
