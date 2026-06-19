using Application.Features.AccessManagement;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Transfers;

public class TransferService : ITransferService
{
    private readonly IApplicationContext dbContext;
    private readonly IAccessService accessService;
    private const int MaxRetries = 5;
    private const int MinExpirationDays = 1;
    private const int MaxExpirationDays = 365;

    public TransferService(IApplicationContext dbContext, IAccessService accessService)
    {
        this.dbContext = dbContext;
        this.accessService = accessService;
    }

    public async Task<VideoTransfer> CreateTransferAsync(
        string userId,
        Guid videoId,
        int expirationDays,
        CancellationToken cancellationToken)
    {
        if (expirationDays < MinExpirationDays || expirationDays > MaxExpirationDays)
        {
            throw new ArgumentException(
                $"Expiration days must be between {MinExpirationDays} and {MaxExpirationDays}.",
                nameof(expirationDays));
        }

        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
        {
            throw new ArgumentException($"Video {videoId} was not found.", nameof(videoId));
        }

        // The sender must personally own the video, and it must be converted.
        if (video.OwnerUserId != userId)
        {
            throw new ArgumentException($"User {userId} does not own video {videoId}.", nameof(videoId));
        }

        if (!video.Converted)
        {
            throw new ArgumentException($"Video {videoId} is not converted yet.", nameof(videoId));
        }

        // The video must be private (a SharedWith row owned by the sender with no event/group).
        var isPrivate = await dbContext.SharedWith
            .AnyAsync(s => s.VideoId == videoId
                        && s.UserId == userId
                        && s.EventId == null
                        && s.GroupId == null, cancellationToken);

        if (!isPrivate)
        {
            throw new ArgumentException("Only private videos can be transferred.", nameof(videoId));
        }

        var now = DateTimeOffset.UtcNow;
        var rollbackCutoff = now.AddDays(-VideoTransfer.RollbackWindowDays);

        // The video may be in at most one active (pending, not-expired) outgoing transfer at a time.
        var alreadyPending = await dbContext.VideoTransferItems
            .Where(i => i.VideoId == videoId
                        && i.Transfer.Status == TransferStatus.Pending
                        && i.Transfer.ExpireAt > now)
            .AnyAsync(cancellationToken);

        if (alreadyPending)
        {
            throw new ArgumentException("This video is already in a pending transfer.", nameof(videoId));
        }

        // The video can't be re-transferred while it's still within a prior transfer's rollback window —
        // a rollback in that window would otherwise have to claw it back from a third party.
        var stillRollbackEligible = await dbContext.VideoTransferItems
            .Where(i => i.VideoId == videoId
                        && i.Transfer.Status == TransferStatus.Accepted
                        && i.Transfer.AcceptedAt > rollbackCutoff)
            .AnyAsync(cancellationToken);

        if (stillRollbackEligible)
        {
            throw new ArgumentException(
                "This video can't be transferred again until it clears a previous transfer's rollback window.",
                nameof(videoId));
        }

        var linkId = await GenerateUniqueLinkIdAsync(cancellationToken);

        var transfer = new VideoTransfer
        {
            Id = linkId,
            CreatedBy = userId,
            CreatedAt = now,
            ExpireAt = now.AddDays(expirationDays),
            Status = TransferStatus.Pending,
            Items = new List<VideoTransferItem>
            {
                new VideoTransferItem
                {
                    Id = Guid.NewGuid(),
                    TransferId = linkId,
                    VideoId = videoId
                }
            }
        };

        dbContext.VideoTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return transfer;
    }

    public async Task<VideoTransfer?> GetTransferAsync(string linkId, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.VideoTransfers
            .Include(t => t.Items)
                .ThenInclude(i => i.Video)
            .FirstOrDefaultAsync(t => t.Id == linkId, cancellationToken);

        if (transfer == null)
        {
            return null;
        }

        // Terminal statuses are always dead.
        if (transfer.Status is TransferStatus.Revoked or TransferStatus.Declined or TransferStatus.RolledBack)
        {
            return null;
        }

        // Only Pending transfers can expire; Accepted stays live.
        if (transfer.Status == TransferStatus.Pending && transfer.ExpireAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return transfer;
    }

    public async Task<IReadOnlyCollection<VideoTransfer>> ListMyOutgoingTransfersAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var transfers = await dbContext.VideoTransfers
            .Include(t => t.Items)
                .ThenInclude(i => i.Video)
            .Where(t => t.CreatedBy == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return transfers.AsReadOnly();
    }

    public async Task<bool> RevokeTransferAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.VideoTransfers
            .FirstOrDefaultAsync(t => t.Id == linkId, cancellationToken);

        if (transfer == null || transfer.CreatedBy != userId)
        {
            return false;
        }

        // Only a pending transfer can be revoked.
        if (transfer.Status != TransferStatus.Pending)
        {
            return false;
        }

        transfer.Status = TransferStatus.Revoked;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeclineTransferAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.VideoTransfers
            .FirstOrDefaultAsync(t => t.Id == linkId, cancellationToken);

        if (transfer == null || transfer.Status != TransferStatus.Pending)
        {
            return false;
        }

        // The sender revokes; the recipient declines.
        if (transfer.CreatedBy == userId)
        {
            return false;
        }

        transfer.Status = TransferStatus.Declined;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AcceptTransferResult> AcceptTransferAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        var transfer = await dbContext.VideoTransfers
            .Include(t => t.Items)
                .ThenInclude(i => i.Video)
            .FirstOrDefaultAsync(t => t.Id == linkId, cancellationToken);

        if (transfer == null
            || transfer.Status != TransferStatus.Pending
            || transfer.ExpireAt <= DateTimeOffset.UtcNow)
        {
            return AcceptTransferResult.NotAvailable;
        }

        if (transfer.CreatedBy == userId)
        {
            return AcceptTransferResult.CannotAcceptOwnTransfer;
        }

        var videoIds = transfer.Items.Select(i => i.VideoId).ToList();

        // Quota check: the recipient's current private usage + the incoming size must fit their quota.
        var requiredBytes = transfer.Items.Sum(i => i.Video.ConvertedBlobSize);
        var usedBytes = await accessService.GetUserPrivateVideosQuery(userId)
            .SumAsync(v => v.ConvertedBlobSize, cancellationToken);
        var recipient = await dbContext.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        var availableBytes = recipient.StorageQuotaBytes - usedBytes;

        if (requiredBytes > availableBytes)
        {
            throw new QuotaExceededException(requiredBytes, availableBytes);
        }

        // Move ownership atomically (single SaveChanges = single transaction).
        foreach (var item in transfer.Items)
        {
            item.Video.OwnerUserId = userId;
        }

        // Re-point the private share rows from the sender to the recipient. SharedWith.UserId is
        // init-only, so replace the rows rather than mutating them. My Library lists private videos
        // by SharedWith.UserId, so this is what actually moves the video between libraries.
        var privateShares = await dbContext.SharedWith
            .Where(s => videoIds.Contains(s.VideoId) && s.EventId == null && s.GroupId == null)
            .ToListAsync(cancellationToken);
        dbContext.SharedWith.RemoveRange(privateShares);
        foreach (var videoId in videoIds)
        {
            dbContext.SharedWith.Add(new SharedWith
            {
                Id = Guid.NewGuid(),
                VideoId = videoId,
                UserId = userId,
                EventId = null,
                GroupId = null
            });
        }

        // Revoke the sender's active share links for the transferred videos.
        var activeLinks = await dbContext.SharedLinks
            .Where(l => videoIds.Contains(l.VideoId) && !l.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var link in activeLinks)
        {
            link.IsRevoked = true;
        }

        transfer.Status = TransferStatus.Accepted;
        transfer.AcceptedByUserId = userId;
        transfer.AcceptedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return AcceptTransferResult.Accepted;
    }

    public async Task<RollbackTransferResult> RollbackTransferAsync(
        string linkId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        var transfer = await dbContext.VideoTransfers
            .Include(t => t.Items)
                .ThenInclude(i => i.Video)
            .FirstOrDefaultAsync(t => t.Id == linkId, cancellationToken);

        if (transfer == null || transfer.Status != TransferStatus.Accepted)
        {
            return RollbackTransferResult.NotAvailable;
        }

        if (transfer.CreatedBy != ownerUserId)
        {
            return RollbackTransferResult.NotOwner;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-VideoTransfer.RollbackWindowDays);
        if (transfer.AcceptedAt <= cutoff)
        {
            return RollbackTransferResult.WindowExpired;
        }

        var sender = transfer.CreatedBy;
        var videoIds = transfer.Items.Select(i => i.VideoId).ToList();

        // Move ownership back to the sender.
        foreach (var item in transfer.Items)
        {
            item.Video.OwnerUserId = sender;
        }

        // Re-point the private share rows back to the sender. The sender's share links that were
        // revoked at acceptance time intentionally stay revoked — the sender can create new ones.
        var privateShares = await dbContext.SharedWith
            .Where(s => videoIds.Contains(s.VideoId) && s.EventId == null && s.GroupId == null)
            .ToListAsync(cancellationToken);
        dbContext.SharedWith.RemoveRange(privateShares);
        foreach (var videoId in videoIds)
        {
            dbContext.SharedWith.Add(new SharedWith
            {
                Id = Guid.NewGuid(),
                VideoId = videoId,
                UserId = sender,
                EventId = null,
                GroupId = null
            });
        }

        transfer.Status = TransferStatus.RolledBack;
        transfer.RolledBackAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return RollbackTransferResult.RolledBack;
    }

    private async Task<string> GenerateUniqueLinkIdAsync(CancellationToken cancellationToken)
    {
        var linkId = ShortLinkGenerator.GenerateShortLinkId();
        var retries = 0;

        while (await dbContext.VideoTransfers.AnyAsync(t => t.Id == linkId, cancellationToken) && retries < MaxRetries)
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
