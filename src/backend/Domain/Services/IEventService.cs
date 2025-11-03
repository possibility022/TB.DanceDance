
using Domain.Entities;

namespace Domain.Services;
public interface IEventService
{
    Task<ICollection<Event>> GetAllEvents(CancellationToken token);
    Task<Event> CreateEventAsync(Event @event);
    IQueryable<Video> GetVideos(Guid eventId, string userId);
    bool IsUserAssignedToEvent(Guid eventId, string userId);
}
