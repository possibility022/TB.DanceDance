using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IUserService
{
    Task AddOrUpdateUserAsync(User user, CancellationToken token);
    Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId, CancellationToken token);
    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId, CancellationToken token);

    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId, CancellationToken token);
    Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId, CancellationToken token);
    Task<ICollection<RequestedAccess>> GetAccessRequestsAsync(string userId, CancellationToken token);
    Task<ICollection<Event>> GetAllEvents(CancellationToken token);
    Task<ICollection<Group>> GetAllGroups(CancellationToken token);
    Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userName, CancellationToken token);

    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events, CancellationToken token);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups, CancellationToken token);
}
