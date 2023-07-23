using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;
public class EventService : IEventService
{
    private readonly DanceDbContext danceDbContext;

    public EventService(DanceDbContext danceDbContext)
    {
        this.danceDbContext = danceDbContext;
    }

    public async Task CreateEventAsync(Event @event, string userId)
    {
        @event.Id = Guid.NewGuid();
        danceDbContext.Events.Add(@event);
        danceDbContext.AssingedToEvents.Add(new AssignedToEvent() { EventId = @event.Id, UserId = userId });
        await danceDbContext.SaveChangesAsync();
    }
}
