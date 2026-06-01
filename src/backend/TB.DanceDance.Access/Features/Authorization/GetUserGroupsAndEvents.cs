using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Authorization;

public class GetUserGroupsAndEventsHandler : IRequestHandler<GetUserGroupsAndEvents, UserGroupsAndEvents>
{
    private readonly AccessDbContext dbContext;

    public GetUserGroupsAndEventsHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<UserGroupsAndEvents> HandleAsync(GetUserGroupsAndEvents request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UserId);
        
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
        
        var groups = from groupAssign in dbContext.AssignedToGroups
            join @group in dbContext.Groups on groupAssign.GroupId equals @group.Id
            where groupAssign.UserId == request.UserId
            select new GroupDto()
            {
                Name = @group.Name,
                Id = @group.Id,
                SeasonEnd = @group.SeasonEnd,
                SeasonStart = @group.SeasonStart,
            };
        ;

        var events = from eventAssign in dbContext.AssignedToEvents
                join @event in dbContext.Events on eventAssign.EventId equals @event.Id
                where eventAssign.UserId == request.UserId
                select @event.MapToDto();
            ;

        var groupsResults = await groups.ToListAsync(cancellationToken);
        var eventsResults = await events.ToListAsync(cancellationToken);

        return new UserGroupsAndEvents() { Events = eventsResults.AsReadOnly(), Groups = groupsResults.AsReadOnly() };
    }
}