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
    
    public async Task<ICollection<Event>> GetAllEvents(CancellationToken token)
    {
        return await dbContext.Events.ToListAsync(token);
    }

    public async Task<Event> CreateEventAsync(Event @event)
    {
        @event.Id = Guid.NewGuid();
        dbContext.Events.Add(@event);
        dbContext.AssingedToEvents.Add(new AssignedToEvent() { EventId = @event.Id, UserId = @event.Owner });
        await dbContext.SaveChangesAsync();
        return @event;
    }

    public IQueryable<Video> GetVideos(Guid eventId, string userId)
    {
        var q = from assignedTo in dbContext.AssingedToEvents
                join sharedWith in dbContext.SharedWith on assignedTo.EventId equals sharedWith.EventId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.EventId == eventId && assignedTo.UserId == userId
                orderby video.RecordedDateTime descending
                select video;

        return q;
    }

    public bool IsUserAssignedToEvent(Guid eventId, string userId)
    {
        return dbContext.AssingedToEvents.Any(r => r.EventId == eventId && r.UserId == userId);
    }
}
