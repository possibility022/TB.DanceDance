using Domain.Entities;
using Domain.Models;
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

    class GroupAndEventQueryResults()
    {
        public Group? Group { get; set; }
        public Event? Event { get; set; }
    }

    public async Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userId)
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

        return (await groups.ToListAsync(), await events.ToListAsync());
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

    public Task AddOrUpdateUserAsync(Domain.Entities.User user)
    {
        var record = dbContext.Users.Find(user.Id);
        if (record != null)
        {
            record.FirstName = user.FirstName;
            record.LastName = user.LastName;
            user.Email = user.Email;
        }
        else
        {
            dbContext.Users.Add(user);
        }

        return dbContext.SaveChangesAsync();
    }

    private IQueryable<RequestedAccess> GetEventRequestsThatCanBeApprovedByUser(string userId)
    {
        var query = from eventRequests in dbContext.EventAssigmentRequests
                    join events in dbContext.Events on eventRequests.EventId equals events.Id
                    join eventRequestor in dbContext.Users on eventRequests.UserId equals eventRequestor.Id
                    where events.Owner == userId && eventRequests.Approved == null
                    select new RequestedAccess
                    {
                        IsGroup = false,
                        RequestId = eventRequests.Id,
                        Name = events.Name,
                        RequestorFirstName = eventRequestor.FirstName,
                        RequestorLastName = eventRequestor.LastName,
                        WhenJoined = null,
                    };

        return query;
    }

    private IQueryable<RequestedAccess> GetGroupRequestsThatCanBeApprovedByUser(string userId)
    {
        var query = from groupRequests in dbContext.GroupAssigmentRequests
                    join groupAdmins in dbContext.GroupsAdmins on groupRequests.GroupId equals groupAdmins.GroupId
                    join groups in dbContext.Groups on groupRequests.GroupId equals groups.Id
                    join groupRequestor in dbContext.Users on groupRequests.UserId equals groupRequestor.Id
                    where groupAdmins.UserId == userId && groupRequests.Approved == null
                    select new RequestedAccess
                    {
                        IsGroup = true,
                        RequestId = groupRequests.Id,
                        Name = groups.Name,
                        RequestorFirstName = groupRequestor.FirstName,
                        RequestorLastName = groupRequestor.LastName,
                        WhenJoined = groupRequests.WhenJoined,
                    };

        return query;
    }

    /// <summary>
    /// Returns events and group ids that user requested access.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserRequests> GetPendingUserRequests(string userId, CancellationToken cancellationToken)
    {
        var eventsRequests = dbContext.EventAssigmentRequests.Where(r => r.UserId == userId && r.Approved != true)
                .Select(r => new { Id = r.EventId, IsEvent = true })
            ;
        var groupRequests = dbContext.GroupAssigmentRequests.Where(r => r.UserId == userId && r.Approved != true)
            .Select(r => new { Id = r.GroupId, IsEvent = false });

        var results = await eventsRequests.Union(groupRequests).ToArrayAsync(cancellationToken);

        return new UserRequests()
        {
            Events = results.Where(r => r.IsEvent == true).Select(r => r.Id).ToArray(),
            Groups = results.Where(r => r.IsEvent == false).Select(r => r.Id).ToArray()
        };
    }
    
    /// <summary>
    /// Returns a list of requests that given user can approve.
    /// </summary>
    /// <param name="userId">User id</param>
    /// <returns></returns>
    public async Task<ICollection<RequestedAccess>> GetAccessRequestsToApproveAsync(string userId)
    {
        var query = (GetEventRequestsThatCanBeApprovedByUser(userId))
                    .Union
                    (GetGroupRequestsThatCanBeApprovedByUser(userId));

        var queryResults = await query.ToListAsync();
        return queryResults;
    }

    public async Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId)
    {
        // todo, how can I make this code simpler? Logic is the same but works on different entities.
        if (isGroup)
        {
            var query = GetGroupRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            var requestRecord = (await dbContext.GroupAssigmentRequests.FindAsync(request.RequestId))!;

            await dbContext.AssingedToGroups.AddAsync(new AssignedToGroup()
            {
                GroupId = requestRecord.GroupId,
                UserId = requestRecord.UserId,
                WhenJoined = requestRecord.WhenJoined,
                Id = Guid.NewGuid(),
            });

            requestRecord.Approve(userId);

            await dbContext.SaveChangesAsync();
        }
        else
        {
            var query = GetEventRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            var requestRecord = (await dbContext.EventAssigmentRequests.FindAsync(request.RequestId))!;

            await dbContext.AssingedToEvents.AddAsync(new AssignedToEvent()
            {
                EventId = requestRecord.EventId,
                UserId = requestRecord.UserId,
                Id = Guid.NewGuid(),
            });

            requestRecord.Approve(userId);

            await dbContext.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId)
    {
        AssigmentRequestBase requestBase;

        if (isGroup)
        {
            var query = GetGroupRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            requestBase = (await dbContext.GroupAssigmentRequests.FindAsync(request.RequestId))!;

        }
        else
        {
            var query = GetEventRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            requestBase = (await dbContext.EventAssigmentRequests.FindAsync(request.RequestId))!;
        }

        requestBase.Decline(userId);
        await dbContext.SaveChangesAsync();

        return true;
    }
}
