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
            .AnyAsync(cancellationToken);
    }

    public Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken)
    {
        return dbContext.AssingedToGroups
            .Where(r => r.UserId == userId && r.GroupId == groupId)
            .AnyAsync(cancellationToken);
    }
    
    public Task<bool> DoesUserHasAccessToEvent(Guid eventId, string userId, CancellationToken cancellationToken)
    {
        return dbContext.AssingedToEvents.AnyAsync(r => r.EventId == eventId && r.UserId == userId, cancellationToken);
    }
    
    private IQueryable<Video> GetBaseVideosForUserQuery(string userId)
    {
        return from video in dbContext.Videos
            join sharedWith in dbContext.SharedWith on video.Id equals sharedWith.VideoId
            join events in dbContext.Events.DefaultIfEmpty() on sharedWith.EventId equals events.Id into eventsGroup
            from events in eventsGroup.DefaultIfEmpty()
            join groups in dbContext.Groups.DefaultIfEmpty() on sharedWith.GroupId equals groups.Id into groupsGroup
            from groups in groupsGroup.DefaultIfEmpty()
            join eventsAssignments in dbContext.AssingedToEvents.DefaultIfEmpty() on events.Id equals eventsAssignments.EventId into eventsAssignmentsGroup
            from eventsAssignments in eventsAssignmentsGroup.DefaultIfEmpty()
            join groupsAssignments in dbContext.AssingedToGroups.DefaultIfEmpty() on groups.Id equals groupsAssignments.GroupId into groupsAssignmentsGroup
            from groupsAssignments in groupsAssignmentsGroup.DefaultIfEmpty()
            where
                sharedWith.UserId == userId || eventsAssignments.UserId == userId || groupsAssignments.UserId == userId && groupsAssignments.WhenJoined < video.RecordedDateTime
            orderby video.RecordedDateTime descending
            select video;
    }

    public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId, CancellationToken cancellationToken)
    {
        var query = GetBaseVideosForUserQuery(userId)
            .Where(v => v.BlobId == videoBlobId)
            .AnyAsync(cancellationToken);

        var any = await query;

        return any;
    }
    
    public async Task<bool> DoesUserHasAccessAsync(Guid videoId, string userId, CancellationToken cancellationToken)
    {
        var query = GetBaseVideosForUserQuery(userId)
            .Where(v => v.Id == videoId)
            .AnyAsync(cancellationToken);

        var any = await query;

        return any;
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