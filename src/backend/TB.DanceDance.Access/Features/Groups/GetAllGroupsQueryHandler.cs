using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Groups;


public class GetAllGroupsQueryHandler : IRequestHandler<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>>
{
    private readonly AccessDbContext dbContext;

    public GetAllGroupsQueryHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<IReadOnlyCollection<GroupDto>> HandleAsync(GetAllGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var array = await dbContext.Groups
            .Select(r => r.MapToDto())
            .ToArrayAsync(cancellationToken);
        
        return array.AsReadOnly();
    }
}