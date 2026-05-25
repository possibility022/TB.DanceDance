using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Authorization;

public class CanUserUpload :
    IRequestHandler<CanUserUploadToEventRequest, bool>,
    IRequestHandler<CanUserUploadToGroupRequest, bool>
{
    private readonly AccessDbContext dbContext;

    public CanUserUpload(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> HandleAsync(CanUserUploadToEventRequest request, CancellationToken cancellationToken = default)
    {
        return dbContext.AssignedToEvents
            .Where(r => r.UserId == request.UserId && r.EventId == request.EventId)
            .AnyAsync(cancellationToken);
    }

    public Task<bool> HandleAsync(CanUserUploadToGroupRequest request, CancellationToken cancellationToken = default)
    {
        return dbContext.AssignedToGroups
            .Where(r => r.UserId == request.UserId && r.GroupId == request.GroupId)
            .AnyAsync(cancellationToken);
    }
}