using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class SaveGroupsAssignmentHandler : IRequestHandler<SaveGroupsAssignmentCommand, bool>
{
    private readonly AccessDbContext dbContext;

    public SaveGroupsAssignmentHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<bool> HandleAsync(SaveGroupsAssignmentCommand request, CancellationToken cancellationToken = default)
    {
        var pendingRequests = await dbContext.GroupAssignmentRequests
            .Where(r => r.UserId == request.UserId && r.Approved == null)
            .Select(r => r.GroupId)
            .ToArrayAsync(cancellationToken);

        var toSave = request.Groups
            .Where(group => !pendingRequests.Contains(group.GroupId))
            .Select(group => GroupAssignmentRequest.Factory.Create(request.UserId, group.GroupId, group.JoinedDate));

        dbContext.GroupAssignmentRequests.AddRange(toSave);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
