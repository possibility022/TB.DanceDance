using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class AccessService : IAccessService
{
    private readonly IApplicationContext dbContext;

    public AccessService(IApplicationContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId, CancellationToken cancellationToken)
    {
        return dbContext.AssingedToEvents
            .Where(r => r.UserId == userId && r.EventId == eventId)
            .AnyAsync();
    }

    public Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken)
    {
        return dbContext.AssingedToGroups
            .Where(r => r.UserId == userId && r.GroupId == groupId)
            .AnyAsync(cancellationToken);
    }
    
    public async Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userId, CancellationToken cancellationToken)
    {
        if (userId is null)
            throw new ArgumentNullException(nameof(userId));


        // For query below I am getting: Unable to translate set operation after client projection has been applied. Consider moving the set operation before the last 'Select' call.
        // I will use two queries for now.
        //var query =
        //    (from groupAssign in dbContext.AssingedToGroups
        //     join @group in dbContext.Groups on groupAssign.GroupId equals @group.Id
        //     where groupAssign.UserId == userId
        //     select new GroupAndEventQueryResults()
        //     {
        //         Event = null,
        //         Group = @group
        //     })
        //    .Union((
        //    from eventAssign in dbContext.AssingedToEvents
        //    join @event in dbContext.Events on eventAssign.EventId equals @event.Id
        //    where eventAssign.UserId == userId
        //    select new GroupAndEventQueryResults()
        //    {
        //        Event = @event,
        //        Group = null
        //    }
        //     ))
        //    .ToList();

        //foreach (var res in query)
        //{
        //    if (res.Group != null)
        //        userGroups.Add(res.Group);

        //    if (res.Event != null)
        //        userEvents.Add(res.Event);
        //}

        var groups = from groupAssign in dbContext.AssingedToGroups
                     join @group in dbContext.Groups on groupAssign.GroupId equals @group.Id
                     where groupAssign.UserId == userId
                     select @group;
        ;

        var events = from eventAssign in dbContext.AssingedToEvents
                     join @event in dbContext.Events on eventAssign.EventId equals @event.Id
                     where eventAssign.UserId == userId
                     select @event
                     ;

        return (await groups.ToListAsync(cancellationToken), await events.ToListAsync(cancellationToken));
    }
}