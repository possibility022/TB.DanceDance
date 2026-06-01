using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class GetPendingUserRequestsHandler : IRequestHandler<GetPendingUserRequestsQuery, UserRequests>
{
    private readonly AccessDbContext dbContext;

    public GetPendingUserRequestsHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<UserRequests> HandleAsync(GetPendingUserRequestsQuery request, CancellationToken cancellationToken = default)
    {
        var eventsRequests = dbContext.EventAssignmentRequests
            .Where(r => r.UserId == request.UserId && r.Approved != true)
            .Select(r => new { Id = r.EventId, IsEvent = true });

        var groupRequests = dbContext.GroupAssignmentRequests
            .Where(r => r.UserId == request.UserId && r.Approved != true)
            .Select(r => new { Id = r.GroupId, IsEvent = false });

        var results = await eventsRequests.Union(groupRequests).ToArrayAsync(cancellationToken);

        return new UserRequests
        {
            Events = results.Where(r => r.IsEvent).Select(r => r.Id).ToArray(),
            Groups = results.Where(r => !r.IsEvent).Select(r => r.Id).ToArray()
        };
    }
}
