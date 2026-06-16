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
        IReadOnlyCollection<Guid> videoIds,
        int expirationDays,
        CancellationToken cancellationToken)
    {
        if (expirationDays < MinExpirationDays || expirationDays > MaxExpirationDays)
        {
            throw new ArgumentException(
                $"Expiration days must be between {MinExpirationDays} and {MaxExpirationDays}.",
                nameof(expirationDays));
        }

        if (videoIds == null || videoIds.Count == 0)
        {
            throw new ArgumentException("At least one video must be selected.", nameof(videoIds));
        }

        var distinctIds = videoIds.Distinct().ToList();
        if (distinctIds.Count != videoIds.Count)
        {
            throw new ArgumentException("Duplicate videos in the transfer.", nameof(videoIds));
        }

        var videos = await dbContext.Videos
            .Where(v => distinctIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        if (videos.Count != distinctIds.Count)
        {
            throw new ArgumentException("One or more videos were not found.", nameof(videoIds));
        }

        // The sender must personally own every video, and each must be converted.
        foreach (var video in videos)
        {
            if (video.UploadedBy != userId)
            {
                throw new ArgumentException($"User {userId} does not own video {video.Id}.", nameof(videoIds));
            }

            if (!video.Converted)
            {
                throw new ArgumentException($"Video {video.Id} is not converted yet.", nameof(videoIds));
            }
        }

        // Each video must be private (a SharedWith row owned by the sender with no event/group).
        var privateVideoIds = await dbContext.SharedWith
            .Where(s => distinctIds.Contains(s.VideoId)
                        && s.UserId == userId
                        && s.EventId == null
                        && s.GroupId == null)
            .Select(s => s.VideoId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (privateVideoIds.Count != distinctIds.Count)
        {
            throw new ArgumentException("Only private videos can be transferred.", nameof(videoIds));
        }

        // A video may be in at most one active (pending, not-expired) outgoing transfer at a time.
        var now = DateTimeOffset.UtcNow;
        var alreadyPending = await dbContext.VideoTransferItems
            .Where(i => distinctIds.Contains(i.VideoId)
                        && i.Transfer.Status == TransferStatus.Pending
                        && i.Transfer.ExpireAt > now)
            .AnyAsync(cancellationToken);

        if (alreadyPending)
        {
            throw new ArgumentException("One or more videos are already in a pending transfer.", nameof(videoIds));
        }

        var linkId = await GenerateUniqueLinkIdAsync(cancellationToken);

        var transfer = new VideoTransfer
        {
            Id = linkId,
            CreatedBy = userId,
            CreatedAt = now,
            ExpireAt = now.AddDays(expirationDays),
            Status = TransferStatus.Pending,
            Items = distinctIds.Select(id => new VideoTransferItem
            {
                Id = Guid.NewGuid(),
                TransferId = linkId,
                VideoId = id
            }).ToList()
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

        // Expired / revoked / declined links are dead.
        if (transfer.ExpireAt <= DateTimeOffset.UtcNow
            || transfer.Status == TransferStatus.Revoked
            || transfer.Status == TransferStatus.Declined)
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
            item.Video.UploadedBy = userId;
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
