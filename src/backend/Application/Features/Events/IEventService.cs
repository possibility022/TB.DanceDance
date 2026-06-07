
using Domain.Entities;

namespace Application.Features.Events;
public interface IEventService
{
    Task<ICollection<Event>> GetAllEvents(CancellationToken cancellationToken);
    Task<Event> CreateEventAsync(Event @event, CancellationToken cancellationToken);
    Task<(IReadOnlyCollection<Video> Items, int TotalCount)> GetVideos(Guid eventId, string userId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
