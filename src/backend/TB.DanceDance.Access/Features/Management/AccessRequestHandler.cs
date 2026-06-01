using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Domain.Entities;
using TB.DanceDance.Access.Infrastructure;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.Access.Features.Management;

class AccessRequestHandler : 
    IRequestHandler<DeclineAccessRequestCommand, bool>,
    IRequestHandler<ApproveAccessRequestCommand, bool>,
    IRequestHandler<GetAccessRequestsToApproveQuery, IReadOnlyCollection<RequestedAccess>>
{
    private readonly AccessDbContext dbContext;

    public AccessRequestHandler(AccessDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<IReadOnlyCollection<RequestedAccess>> HandleAsync(GetAccessRequestsToApproveQuery request, CancellationToken cancellationToken = default)
    {
        var query = GetEventRequestsThatCanBeApprovedByUser(request.UserId)
            .Union(GetGroupRequestsThatCanBeApprovedByUser(request.UserId));

        return await query.ToListAsync(cancellationToken);
    }
    
    public async Task<bool> HandleAsync(ApproveAccessRequestCommand request, CancellationToken cancellationToken = default)
    {
        if (request.IsGroup)
        {
            var accessRequest = await GetGroupRequestsThatCanBeApprovedByUser(request.UserId)
                .Where(r => r.RequestId == request.RequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (accessRequest == null)
                return false;

            var requestRecord = (await dbContext.GroupAssignmentRequests.FindAsync([accessRequest.RequestId], cancellationToken))!;
            dbContext.AssignedToGroups.Add(AssignedToGroup.Factory.Create(requestRecord.GroupId, requestRecord.UserId, requestRecord.WhenJoined));
            requestRecord.Approve(request.UserId);
        }
        else
        {
            var accessRequest = await GetEventRequestsThatCanBeApprovedByUser(request.UserId)
                .Where(r => r.RequestId == request.RequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (accessRequest == null)
                return false;

            var requestRecord = (await dbContext.EventAssignmentRequests.FindAsync([accessRequest.RequestId], cancellationToken))!;
            dbContext.AssignedToEvents.Add(AssignedToEvent.Factory.Create(requestRecord.EventId, requestRecord.UserId));
            requestRecord.Approve(request.UserId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HandleAsync(DeclineAccessRequestCommand request, CancellationToken cancellationToken = default)
    {
        AssigmentRequestBase requestBase;

        if (request.IsGroup)
        {
            var accessRequest = await GetGroupRequestsThatCanBeApprovedByUser(request.UserId)
                .Where(r => r.RequestId == request.RequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (accessRequest == null)
                return false;

            requestBase = (await dbContext.GroupAssignmentRequests.FindAsync([accessRequest.RequestId], cancellationToken))!;
        }
        else
        {
            var accessRequest = await GetEventRequestsThatCanBeApprovedByUser(request.UserId)
                .Where(r => r.RequestId == request.RequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (accessRequest == null)
                return false;

            requestBase = (await dbContext.EventAssignmentRequests.FindAsync([accessRequest.RequestId], cancellationToken))!;
        }

        requestBase.Decline(request.UserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<RequestedAccess> GetEventRequestsThatCanBeApprovedByUser(string userId) =>
        from eventRequests in dbContext.EventAssignmentRequests
        join events in dbContext.Events on eventRequests.EventId equals events.Id
        join eventRequestor in dbContext.Users on eventRequests.UserId equals eventRequestor.Id
        where events.Owner == userId && eventRequests.Approved == null
        select new RequestedAccess
        {
            IsGroup = false,
            RequestId = eventRequests.Id,
            Name = events.Name,
            RequestorFirstName = eventRequestor.FirstName,
            RequestorLastName = eventRequestor.LastName,
            WhenJoined = null,
        };

    private IQueryable<RequestedAccess> GetGroupRequestsThatCanBeApprovedByUser(string userId) =>
        from groupRequests in dbContext.GroupAssignmentRequests
        join groupAdmins in dbContext.GroupsAdmins on groupRequests.GroupId equals groupAdmins.GroupId
        join groups in dbContext.Groups on groupRequests.GroupId equals groups.Id
        join groupRequestor in dbContext.Users on groupRequests.UserId equals groupRequestor.Id
        where groupAdmins.UserId == userId && groupRequests.Approved == null
        select new RequestedAccess
        {
            IsGroup = true,
            RequestId = groupRequests.Id,
            Name = groups.Name,
            RequestorFirstName = groupRequestor.FirstName,
            RequestorLastName = groupRequestor.LastName,
            WhenJoined = groupRequests.WhenJoined,
        };


}
