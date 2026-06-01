using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Events;

class GetAllEventsQueryHandler : IRequestHandler<GetAllEventsQuery, IReadOnlyCollection<EventDto>>
{
    private readonly AccessDbContext dbContext;

    public GetAllEventsQueryHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<EventDto>> HandleAsync(GetAllEventsQuery request, CancellationToken cancellationToken = default)
    {
        var events = await dbContext.Events
            .Select(e => e.MapToDto())
            .ToArrayAsync(cancellationToken);

        return events.AsReadOnly();
    }
}
