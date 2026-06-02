
using Domain.Entities;

namespace Application.Features.Events;
public interface IEventService
{
    Task<ICollection<Event>> GetAllEvents(CancellationToken cancellationToken);
    Task<Event> CreateEventAsync(Event @event, CancellationToken cancellationToken);
    Task<Video[]> GetVideos(Guid eventId, string userId, CancellationToken cancellationToken);
}
