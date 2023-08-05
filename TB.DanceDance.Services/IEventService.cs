using TB.DanceDance.Data.PostgreSQL.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;
public interface IEventService
{
    Task<Event> CreateEventAsync(Event @event, string userId);
    IQueryable<VideoInfo> GetVideos(Guid eventId, string userId);
}
