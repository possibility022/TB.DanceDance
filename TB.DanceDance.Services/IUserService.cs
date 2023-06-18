using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;

public interface IUserService
{

    Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId);

    Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId);

    Task<ICollection<Event>> GetAllEvents(string user);
    Task<ICollection<Group>> GetAllGroups(string user);
    IQueryable<Group> GetUserGroups(string userId);
    IQueryable<Event> GetUserEvents(string userId);
    (ICollection<Group>, ICollection<Event>) GetUserEventsAndGroups(string userName);

    Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events);
    Task SaveGroupsAssigmentRequests(string user, ICollection<Guid> groups);
}
