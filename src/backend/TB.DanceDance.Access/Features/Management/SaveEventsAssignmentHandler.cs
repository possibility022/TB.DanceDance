using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class SaveEventsAssignmentHandler : IRequestHandler<SaveEventsAssignmentCommand, bool>
{
    private readonly AccessDbContext dbContext;

    public SaveEventsAssignmentHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> HandleAsync(SaveEventsAssignmentCommand request, CancellationToken cancellationToken = default)
    {
        var pendingRequests = await dbContext.EventAssignmentRequests
            .Where(r => r.UserId == request.UserId && r.Approved == null)
            .Select(r => r.EventId)
            .ToArrayAsync(cancellationToken);

        var toSave = request.Events
            .Except(pendingRequests)
            .Select(@event => EventAssignmentRequest.Factory.Create(request.UserId, @event));

        dbContext.EventAssignmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
