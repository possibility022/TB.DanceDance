using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class GetUsersByIdsHandler : IRequestHandler<GetUsersByIdsQuery, IReadOnlyCollection<UserInfoDto>>
{
    private readonly AccessDbContext dbContext;

    public GetUsersByIdsHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserInfoDto>> HandleAsync(GetUsersByIdsQuery request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.UserIds.Count == 0)
            return Array.Empty<UserInfoDto>();

        var ids = request.UserIds.Distinct().ToArray();

        var users = await dbContext.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserInfoDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
            })
            .ToArrayAsync(cancellationToken);

        return users.AsReadOnly();
    }
}
