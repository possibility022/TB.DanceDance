using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Groups;

class GetUserGroupMembershipsQueryHandler
    : IRequestHandler<GetUserGroupMembershipsQuery, IReadOnlyCollection<GroupMembershipDto>>
{
    private readonly AccessDbContext dbContext;

    public GetUserGroupMembershipsQueryHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<GroupMembershipDto>> HandleAsync(GetUserGroupMembershipsQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.UserId);

        var memberships = await dbContext.AssignedToGroups
            .Where(r => r.UserId == request.UserId)
            .Select(r => new GroupMembershipDto
            {
                GroupId = r.GroupId,
                WhenJoined = r.WhenJoined,
            })
            .ToArrayAsync(cancellationToken);

        return memberships.AsReadOnly();
    }
}
