using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IUserService
{
    Task AddOrUpdateUserAsync(User user);
    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId);

    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId);
    Task<ICollection<AccessRequests>> GetAccessRequestsAsync(string userId);
    Task<ICollection<Event>> GetAllEvents();
    Task<ICollection<Group>> GetAllGroups();
    (ICollection<Group>, ICollection<Event>) GetUserEventsAndGroups(string userName);

    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups);
}
