using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;
public interface IEventService
{
    Task<Event> CreateEventAsync(Event @event, string userId);
    IQueryable<Video> GetVideos(Guid eventId, string userId);
    bool IsUserAssignedToEvent(Guid eventId, string userId);
    bool IsUserAssignedToGroup(Guid groupId, string userId);
}
