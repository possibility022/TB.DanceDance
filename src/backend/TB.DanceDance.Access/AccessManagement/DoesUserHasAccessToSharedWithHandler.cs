using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Access.AccessManagement;

class DoesUserHasAccessToSharedWithHandler : IRequestHandler<DoesUserHasAccessToSharedWith, bool>
{
    private readonly AccessDbContext dbContext;

    public DoesUserHasAccessToSharedWithHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public Task<bool> HandleAsync(DoesUserHasAccessToSharedWith request, CancellationToken cancellationToken = default)
    {
        if (request.SharedWithType == SharedWithByType.Event)
        {
            var hasAccessByEvent = QueryToCheckForEventTable(request.UserId, request.SharedToId);
            return hasAccessByEvent;
        } else if (request.SharedWithType == SharedWithByType.Group)
        {
            ArgumentNullException.ThrowIfNull(request.WhenJoined);
            var hasAccessByGroup = QueryToCheckForGroupTable(request.UserId, request.SharedToId, request.WhenJoined.Value);
            return hasAccessByGroup;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(request.SharedWithType));
        }
    }

    private Task<bool> QueryToCheckForGroupTable(string requestUserId, Guid requestSharedToId, DateTime whenJoined)
    {
        var sharedByGroups = 
            from assignedToGroup in dbContext.AssignedToGroups
            where assignedToGroup.UserId == requestUserId
                  && assignedToGroup.WhenJoined >=  whenJoined
                  && assignedToGroup.GroupId == requestSharedToId
            select assignedToGroup;
        
        return sharedByGroups.AnyAsync();
    }

    private Task<bool> QueryToCheckForEventTable(string requestUserId, Guid requestSharedToId)
    {
        var sharedByEvents = 
            from eventsAccess in dbContext.AssignedToEvents
            where eventsAccess.UserId == requestUserId
                  && eventsAccess.EventId == requestSharedToId
            select eventsAccess;
        
        return sharedByEvents.AnyAsync();
    }
}