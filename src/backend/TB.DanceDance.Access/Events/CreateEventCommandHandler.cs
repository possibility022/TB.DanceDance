using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Events;

public record CreateEventCommand(string Name, DateTime Date, string OwnerId) : IRequest<Guid>;

class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Guid>
{
    private readonly AccessDbContext dbContext;

    public CreateEventCommandHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<Guid> HandleAsync(CreateEventCommand request, CancellationToken cancellationToken = default)
    {
        var @event = Event.Factory.Create(request.Name, request.Date, EventType.Unknown, request.OwnerId);
        var assignedToEvent = AssignedToEvent.Factory.Create(@event.Id, @event.Owner);
        dbContext.Events.Add(@event);
        dbContext.AssignedToEvents.Add(assignedToEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return @event.Id;
    }
}