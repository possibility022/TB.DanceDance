using Domain.Entities;

namespace Domain.Services;

public interface IUserService
{

    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId);

    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId);

    Task<ICollection<Event>> GetAllEvents();
    Task<ICollection<Group>> GetAllGroups();
    (ICollection<Group>, ICollection<Event>) GetUserEventsAndGroups(string userName);

    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events, string userDisplayName);
    Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups, string userDisplayName);
}
