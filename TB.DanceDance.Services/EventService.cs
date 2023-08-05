using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;
public class EventService : IEventService
{
    private readonly DanceDbContext dbContext;

    public EventService(DanceDbContext danceDbContext)
    {
        this.dbContext = danceDbContext;
    }

    public async Task<Event> CreateEventAsync(Event @event, string userId)
    {
        @event.Id = Guid.NewGuid();
        dbContext.Events.Add(@event);
        dbContext.AssingedToEvents.Add(new AssignedToEvent() { EventId = @event.Id, UserId = userId });
        await dbContext.SaveChangesAsync();
        return @event;
    }

    public IQueryable<VideoInfo> GetVideos(Guid eventId, string userId)
    {
        var q = from assignedTo in dbContext.AssingedToEvents
                join sharedWith in dbContext.SharedWith on assignedTo.EventId equals sharedWith.EventId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.EventId == eventId && assignedTo.UserId == userId
                select new VideoInfo()
                {
                    SharedWithEvent = true,
                    SharedWithGroup = false,
                    Video = video
                };

        return q;
    }
}
