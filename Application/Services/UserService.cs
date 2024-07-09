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

    public Task<bool> CanUserUploadToEventAsync(string userId, Guid eventId, CancellationToken token)
    {
        return dbContext.AssingedToEvents
            .Where(r => r.UserId == userId && r.EventId == eventId)
            .AnyAsync(token);
    }

    public Task<bool> CanUserUploadToGroupAsync(string userId, Guid groupId, CancellationToken token)
    {
        return dbContext.AssingedToGroups
            .Where(r => r.UserId == userId && r.GroupId == groupId)
            .AnyAsync(token);
    }

    class GroupAndEventQueryResults()
    {
        public Group? Group { get; set; }
        public Event? Event { get; set; }
    }

    public async Task<(ICollection<Group>, ICollection<Event>)> GetUserEventsAndGroupsAsync(string userId, CancellationToken token)
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

        return (await groups.ToListAsync(token), await events.ToListAsync(token));
    }

    public async Task<ICollection<Event>> GetAllEvents(CancellationToken token)
    {
        return await dbContext.Events.ToListAsync(token);
    }

    public async Task<ICollection<Group>> GetAllGroups(CancellationToken token)
    {
        return await dbContext.Groups.ToListAsync(token);
    }

    public async Task SaveEventsAssigmentRequest(string user, ICollection<Guid> events, CancellationToken token)
    {
        var toSave = events.Select(@event => new EventAssigmentRequest()
        {
            EventId = @event,
            UserId = user
        });

        dbContext.EventAssigmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync(token);
    }

    public async Task SaveGroupsAssigmentRequests(string user, ICollection<(Guid groupId, DateTime joinedDate)> groups, CancellationToken token)
    {
        var toSave = groups.Select(group => new GroupAssigmentRequest()
        {
            GroupId = group.groupId,
            WhenJoined = group.joinedDate,
            UserId = user
        });

        dbContext.GroupAssigmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync(token);
    }

    public Task AddOrUpdateUserAsync(Domain.Entities.User user, CancellationToken token)
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

        return dbContext.SaveChangesAsync(token);
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

    public async Task<ICollection<RequestedAccess>> GetAccessRequestsAsync(string userId, CancellationToken token)
    {
        var query = (GetEventRequestsThatCanBeApprovedByUser(userId))
                    .Union
                    (GetGroupRequestsThatCanBeApprovedByUser(userId));

        var queryResults = await query.ToListAsync(token);
        return queryResults;
    }

    public async Task<bool> ApproveAccessRequest(Guid requestId, bool isGroup, string userId, CancellationToken token)
    {
        // todo, how can I make this code simpler? Logic is the same but works on different entities.
        if (isGroup)
        {
            var query = GetGroupRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync(token);

            if (request == null)
                return false;

            var requestRecord = (await dbContext.GroupAssigmentRequests.FindAsync(request.RequestId, token))!;

            await dbContext.AssingedToGroups.AddAsync(new AssignedToGroup()
            {
                GroupId = requestRecord.GroupId,
                UserId = requestRecord.UserId,
                WhenJoined = requestRecord.WhenJoined,
                Id = Guid.NewGuid(),
            }, token);

            requestRecord.Approve(userId);

            await dbContext.SaveChangesAsync(token);
        }
        else
        {
            var query = GetEventRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            var requestRecord = (await dbContext.EventAssigmentRequests.FindAsync(request.RequestId, token))!;

            await dbContext.AssingedToEvents.AddAsync(new AssignedToEvent()
            {
                EventId = requestRecord.EventId,
                UserId = requestRecord.UserId,
                Id = Guid.NewGuid(),
            }, token);

            requestRecord.Approve(userId);

            await dbContext.SaveChangesAsync(token);
        }

        return true;
    }

    public async Task<bool> DeclineAccessRequest(Guid requestId, bool isGroup, string userId, CancellationToken token)
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

            requestBase = (await dbContext.GroupAssigmentRequests.FindAsync(request.RequestId, token))!;

        }
        else
        {
            var query = GetEventRequestsThatCanBeApprovedByUser(userId);
            var request = await query
                .Where(r => r.RequestId == requestId)
                .FirstOrDefaultAsync();

            if (request == null)
                return false;

            requestBase = (await dbContext.EventAssigmentRequests.FindAsync(request.RequestId, token))!;
        }

        requestBase.Decline(userId);
        await dbContext.SaveChangesAsync(token);

        return true;
    }
}
