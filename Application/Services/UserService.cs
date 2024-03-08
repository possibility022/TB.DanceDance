using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationContext dbContext;

    public UserService(IApplicationContext dbContext
        )
    {
        this.dbContext = dbContext;
    }

    public Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId)
    {
        return dbContext.AssingedToEvents
            .Where(r => r.UserId == userId && r.EventId == eventId)
            .AnyAsync();
    }

    public Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId)
    {
        return dbContext.AssingedToGroups
            .Where(r => r.UserId == userId && r.GroupId == groupId)
            .AnyAsync();
    }

    public (ICollection<Group>, ICollection<Event>) GetUserEventsAndGroups(string userId)
    {
        if (userId is null)
            throw new ArgumentNullException(nameof(userId));

        var userGroups = new HashSet<Group>();
        var userEvents = new HashSet<Event>();

        var query =
            from groupAssign in dbContext.AssingedToGroups
            join @group in dbContext.Groups on groupAssign.GroupId equals @group.Id
            where groupAssign.UserId == userId
            from eventAssign in dbContext.AssingedToEvents
            join @event in dbContext.Events on eventAssign.EventId equals @event.Id
            where eventAssign.UserId == userId
            select new { Group = @group, Event = @event };

        foreach (var res in query)
        {
            if (res.Group != null)
                userGroups.Add(res.Group);

            if (res.Event != null)
                userEvents.Add(res.Event);
        }

        return (userGroups, userEvents);
    }

    public async Task<ICollection<Event>> GetAllEvents()
    {
        return await dbContext.Events.ToListAsync();
    }

    public async Task<ICollection<Group>> GetAllGroups()
    {
        return await dbContext.Groups.ToListAsync();
    }

    public async Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events)
    {
        var toSave = events.Select(@event => new EventAssigmentRequest()
        {
            EventId = @event,
            UserId = user
        });

        dbContext.EventAssigmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups)
    {
        var toSave = groups.Select(group => new GroupAssigmentRequest()
        {
            GroupId = group.groupId,
            WhenJoined = group.joinedDate,
            UserId = user
        });

        dbContext.GroupAssigmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync();
    }
}
