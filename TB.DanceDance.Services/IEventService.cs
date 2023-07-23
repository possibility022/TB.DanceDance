using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;
public interface IEventService
{
    Task CreateEventAsync(Event @event, string userId);
}
