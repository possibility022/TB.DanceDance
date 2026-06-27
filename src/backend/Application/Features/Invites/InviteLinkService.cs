using Application.Features.Groups;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invites;

public class InviteLinkService : IInviteLinkService
{
    private readonly IApplicationContext dbContext;
    private readonly IGroupService groupService;
    private const int MaxRetries = 5;

    public InviteLinkService(IApplicationContext dbContext, IGroupService groupService)
    {
        this.dbContext = dbContext;
        this.groupService = groupService;
    }

    public async Task<InviteLink> CreateForGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        if (!await groupService.IsGroupAdmin(groupId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not an admin of group {groupId}.");
        }

        return await CreateAsync(groupId, null, userId, cancellationToken);
    }

    public async Task<InviteLink> CreateForEventAsync(Guid eventId, string userId, CancellationToken cancellationToken)
    {
        if (!await IsEventAdminAsync(eventId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not an admin of event {eventId}.");
        }

        return await CreateAsync(null, eventId, userId, cancellationToken);
    }

    public async Task<InviteLinkInfo?> GetInfoAsync(string linkId, CancellationToken cancellationToken)
    {
        var link = await dbContext.InviteLinks.FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);
        if (link == null)
        {
            return null;
        }

        var isRedeemable = link.Status == InviteLinkStatus.Active && link.ExpireAt > DateTimeOffset.UtcNow;

        string targetType;
        string targetName;
        if (link.GroupId.HasValue)
        {
            targetType = "Group";
            targetName = await dbContext.Groups
                .Where(g => g.Id == link.GroupId)
                .Select(g => g.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }
        else
        {
            targetType = "Event";
            targetName = await dbContext.Events
                .Where(e => e.Id == link.EventId)
                .Select(e => e.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        }

        return new InviteLinkInfo(link.Id, targetType, targetName, isRedeemable);
    }

    public async Task<RedeemInviteLinkResult> RedeemAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        // AsNoTracking: the actual mutation below goes through ExecuteUpdateAsync, which bypasses
        // the change tracker. A tracked read here could go stale (e.g. if this status check were
        // ever re-run against the same DbContext after a prior redemption) and wrongly fall through
        // to the already-member branch instead of correctly rejecting the attempt.
        var link = await dbContext.InviteLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (link == null || link.Status != InviteLinkStatus.Active || link.ExpireAt <= now)
        {
            return RedeemInviteLinkResult.NotAvailable;
        }

        var alreadyMember = link.GroupId.HasValue
            ? await dbContext.AssingedToGroups.AnyAsync(a => a.GroupId == link.GroupId && a.UserId == userId, cancellationToken)
            : await dbContext.AssingedToEvents.AnyAsync(a => a.EventId == link.EventId && a.UserId == userId, cancellationToken);

        if (alreadyMember)
        {
            return RedeemInviteLinkResult.AlreadyMember;
        }

        // Atomic conditional update: exactly one of two concurrent callers gets rowsAffected == 1.
        var rowsAffected = await dbContext.InviteLinks
            .Where(l => l.Id == linkId && l.Status == InviteLinkStatus.Active)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.Status, InviteLinkStatus.Redeemed)
                .SetProperty(l => l.RedeemedByUserId, userId)
                .SetProperty(l => l.RedeemedAt, now), cancellationToken);

        if (rowsAffected == 0)
        {
            return RedeemInviteLinkResult.NotAvailable;
        }

        if (link.GroupId.HasValue)
        {
            dbContext.AssingedToGroups.Add(new AssignedToGroup
            {
                Id = Guid.NewGuid(),
                GroupId = link.GroupId.Value,
                UserId = userId,
                WhenJoined = now.UtcDateTime,
            });
        }
        else
        {
            dbContext.AssingedToEvents.Add(new AssignedToEvent
            {
                Id = Guid.NewGuid(),
                EventId = link.EventId!.Value,
                UserId = userId,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return RedeemInviteLinkResult.Redeemed;
    }

    public async Task<IReadOnlyCollection<InviteLink>> ListForGroupAsync(Guid groupId, string userId, CancellationToken cancellationToken)
    {
        if (!await groupService.IsGroupAdmin(groupId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not an admin of group {groupId}.");
        }

        var links = await dbContext.InviteLinks
            .Where(l => l.GroupId == groupId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<InviteLink>> ListForEventAsync(Guid eventId, string userId, CancellationToken cancellationToken)
    {
        if (!await IsEventAdminAsync(eventId, userId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User {userId} is not an admin of event {eventId}.");
        }

        var links = await dbContext.InviteLinks
            .Where(l => l.EventId == eventId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.AsReadOnly();
    }

    public async Task<RevokeInviteLinkResult> RevokeAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        var link = await dbContext.InviteLinks.FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);
        if (link == null)
        {
            return RevokeInviteLinkResult.NotFound;
        }

        var isAdmin = link.GroupId.HasValue
            ? await groupService.IsGroupAdmin(link.GroupId.Value, userId, cancellationToken)
            : await IsEventAdminAsync(link.EventId!.Value, userId, cancellationToken);

        if (!isAdmin)
        {
            return RevokeInviteLinkResult.NotAuthorized;
        }

        if (link.Status == InviteLinkStatus.Redeemed)
        {
            return RevokeInviteLinkResult.AlreadyRedeemed;
        }

        if (link.Status == InviteLinkStatus.Active)
        {
            link.Status = InviteLinkStatus.Revoked;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return RevokeInviteLinkResult.Revoked;
    }

    private Task<bool> IsEventAdminAsync(Guid eventId, string userId, CancellationToken cancellationToken)
    {
        return dbContext.Events.AnyAsync(e => e.Id == eventId && e.Owner == userId, cancellationToken);
    }

    private async Task<InviteLink> CreateAsync(Guid? groupId, Guid? eventId, string userId, CancellationToken cancellationToken)
    {
        var linkId = await GenerateUniqueLinkIdAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var link = new InviteLink
        {
            Id = linkId,
            GroupId = groupId,
            EventId = eventId,
            CreatedBy = userId,
            CreatedAt = now,
            ExpireAt = now.AddDays(InviteLink.ExpirationDays),
            Status = InviteLinkStatus.Active,
        };

        dbContext.InviteLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);

        return link;
    }

    private async Task<string> GenerateUniqueLinkIdAsync(CancellationToken cancellationToken)
    {
        var linkId = ShortLinkGenerator.GenerateShortLinkId();
        var retries = 0;

        while (await dbContext.InviteLinks.AnyAsync(l => l.Id == linkId, cancellationToken) && retries < MaxRetries)
        {
            linkId = ShortLinkGenerator.GenerateShortLinkId();
            retries++;
        }

        if (retries >= MaxRetries)
        {
            throw new InvalidOperationException("Failed to generate unique link ID after maximum retries.");
        }

        return linkId;
    }
}
