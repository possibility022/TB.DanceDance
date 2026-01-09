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

    public async Task<IReadOnlyCollection<Video>> GetUserPrivateVideos(string userId, CancellationToken cancellationToken)
    {
        var privateVideos = dbContext.SharedWith
            .Where(r => r.EventId == null && r.GroupId == null && r.UserId == userId)
            .Join(dbContext.Videos, v => v.VideoId, v => v.Id, (c, v) => v)
            .AsNoTracking();

        var result = await privateVideos.ToArrayAsync(cancellationToken);
        return result;
    }
    
    private IQueryable<Video> GetBaseVideosForUserQuery(string userId)
    {
        var sharedByEvents = 
            from eventsAccess in dbContext.AssingedToEvents
            where eventsAccess.UserId == userId
            join SharedWith in dbContext.SharedWith
                on eventsAccess.EventId equals SharedWith.EventId into sharedWithEvents
            from sharedWith in sharedWithEvents
            join videos in dbContext.Videos
                on sharedWith.VideoId equals videos.Id
            select videos;
        
        var sharedByGroups = 
            from groupAccess in dbContext.AssingedToGroups
            where groupAccess.UserId == userId
            join SharedWith in dbContext.SharedWith
                on groupAccess.GroupId equals SharedWith.GroupId into sharedWithGroups
            from sharedWith in sharedWithGroups
            join videos in dbContext.Videos
                on sharedWith.VideoId equals videos.Id
            select videos;
        
        var accessibleAsPrivate = 
            from privateVideos in dbContext.SharedWith
            where privateVideos.UserId == userId && privateVideos.EventId == null && privateVideos.GroupId == null
            join videos in dbContext.Videos
                on privateVideos.VideoId equals videos.Id
            select videos;

        var v = dbContext.SharedWith.ToArray();
        
        var accessibleVideos = sharedByEvents
            .Union(sharedByGroups)
            .Union(accessibleAsPrivate)
            .Distinct();

        return accessibleVideos;
    }

    public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId, CancellationToken cancellationToken)
    {
        // TODO: Implement storage quota enforcement for private videos at VIEW/STREAM time
        // For private videos (SharedWith.EventId == null && SharedWith.GroupId == null):
        // 1. Calculate total ConvertedBlobSize for user's private videos
        // 2. Compare against User.StorageQuotaBytes
        // 3. If over quota, deny access (user can list but not view)
        // This ensures users can upload but cannot view videos beyond their quota limit

        var query = GetBaseVideosForUserQuery(userId)
            .Where(v => v.BlobId == videoBlobId)
            .AnyAsync(cancellationToken);

        var any = await query;

        return any;
    }

    public async Task<bool> DoesUserHasAccessAsync(Guid videoId, string userId, CancellationToken cancellationToken)
    {
        // TODO: Implement storage quota enforcement for private videos at VIEW/STREAM time
        // Same logic as DoesUserHasAccessAsync(videoBlobId) above

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