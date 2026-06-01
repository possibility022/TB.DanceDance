using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Groups;

public class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GroupDto?>
{
    private readonly AccessDbContext dbContext;

    public GetGroupByIdQueryHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }


    public async Task<GroupDto?> HandleAsync(GetGroupByIdQuery request, CancellationToken cancellationToken = default)
    {
        var res = await dbContext.Groups
            .Where(r => r.Id == request.Id)
            .Select(g => g.MapToDto())
            .FirstOrDefaultAsync(cancellationToken);

        return res;
    }
}