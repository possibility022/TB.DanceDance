using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;
public class EventService : IEventService
{
    private readonly IApplicationContext dbContext;

    public EventService(IApplicationContext danceDbContext)
    {
        dbContext = danceDbContext;
    }
    
    public async Task<ICollection<Event>> GetAllEvents(CancellationToken cancellationToken)
    {
        return await dbContext.Events.ToListAsync(cancellationToken);
    }

    public async Task<Event> CreateEventAsync(Event @event, CancellationToken cancellationToken)
    {
        @event.Id = Guid.NewGuid();
        dbContext.Events.Add(@event);
        dbContext.AssingedToEvents.Add(new AssignedToEvent() { EventId = @event.Id, UserId = @event.Owner });
        await dbContext.SaveChangesAsync(cancellationToken);
        return @event;
    }

    public Task<Video[]> GetVideos(Guid eventId, string userId, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToEvents
                join sharedWith in dbContext.SharedWith on assignedTo.EventId equals sharedWith.EventId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.EventId == eventId && assignedTo.UserId == userId
                orderby video.RecordedDateTime descending
                select video;

        return q.ToArrayAsync(cancellationToken);
    }
}
