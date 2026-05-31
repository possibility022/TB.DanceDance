using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Access.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Groups;


record GetAllUserGroupsQuery : IRequest<IReadOnlyCollection<AssignedGroupDto>>
{
    public string UserId { get; init; }
}

class GetAllUserGroupsQueryHandler : IRequestHandler<GetAllUserGroupsQuery, IReadOnlyCollection<AssignedGroupDto>>
{
    private readonly AccessDbContext dbContext;

    public GetAllUserGroupsQueryHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<IReadOnlyCollection<AssignedGroupDto>> HandleAsync(GetAllUserGroupsQuery request, CancellationToken cancellationToken = default)
    {
        var array = await dbContext.AssignedToGroups
            .Join(dbContext.Groups, ag => ag.GroupId, g => g.Id, (ag, g) => new {ag, g})
            .Where(r => r.ag.UserId == request.UserId)
            .Select(r => r.g.MapToAssignedGroupDto(r.ag.WhenJoined))
            .ToArrayAsync(cancellationToken);
        
        return array.AsReadOnly();
    }
}