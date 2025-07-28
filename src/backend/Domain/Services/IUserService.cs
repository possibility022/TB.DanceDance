using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IUserService
{
    Task AddOrUpdateUserAsync(User user);
    Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId);
    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId);

    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId);
    Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId);
    Task<UserRequests> GetPendingUserRequests(string userId, CancellationToken cancellationToken);
    Task<ICollection<RequestedAccess>> GetAccessRequestsToApproveAsync(string userId);
    Task<ICollection<Event>> GetAllEvents();
    Task<ICollection<Group>> GetAllGroups();
    Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userName);

    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups);
}
