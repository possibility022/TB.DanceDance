using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class SharedLinkService : ISharedLinkService
{
    private readonly IApplicationContext dbContext;
    private readonly IAccessService accessService;
    private const int MaxRetries = 5;
    private const int MinExpirationDays = 1;
    private const int MaxExpirationDays = 365;

    public SharedLinkService(IApplicationContext dbContext, IAccessService accessService)
    {
        this.dbContext = dbContext;
        this.accessService = accessService;
    }

    public async Task<SharedLink> CreateSharedLinkAsync(
        Guid videoId,
        string userId,
        int expirationDays,
        bool allowComments,
        bool allowAnonymousComments,
        CancellationToken cancellationToken)
    {
        // Validate expiration days
        if (expirationDays < MinExpirationDays || expirationDays > MaxExpirationDays)
        {
            throw new ArgumentException(
                $"Expiration days must be between {MinExpirationDays} and {MaxExpirationDays}.",
                nameof(expirationDays));
        }

        // Check if video exists
        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
        {
            throw new ArgumentException($"Video with ID {videoId} not found.", nameof(videoId));
        }

        // Check if user can share this video
        var canShare = await CanUserShareVideoAsync(videoId, userId, cancellationToken);
        if (!canShare)
        {
            throw new ArgumentException(
                $"User {userId} is not authorized to share video {videoId}.",
                nameof(userId));
        }

        // Generate unique short link ID with collision retry
        string linkId = GenerateUniqueLinkId();
        int retries = 0;

        while (await dbContext.SharedLinks.AnyAsync(l => l.Id == linkId, cancellationToken) && retries < MaxRetries)
        {
            linkId = GenerateUniqueLinkId();
            retries++;
        }

        if (retries >= MaxRetries)
        {
            throw new InvalidOperationException("Failed to generate unique link ID after maximum retries.");
        }

        // Create shared link
        var now = DateTimeOffset.UtcNow;
        var sharedLink = new SharedLink
        {
            Id = linkId,
            VideoId = videoId,
            SharedBy = userId,
            CreatedAt = now,
            ExpireAt = now.AddDays(expirationDays),
            IsRevoked = false,
            AllowComments = allowComments,
            AllowAnonymousComments = allowAnonymousComments
        };

        dbContext.SharedLinks.Add(sharedLink);
        await dbContext.SaveChangesAsync(cancellationToken);

        return sharedLink;
    }

    public async Task<Video?> GetVideoBySharedLinkAsync(string linkId, CancellationToken cancellationToken)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null)
        {
            return null;
        }

        // Validate link is not expired and not revoked
        if (link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            return null;
        }

        return link.Video;
    }

    public async Task<bool> RevokeSharedLinkAsync(string linkId, string userId, CancellationToken cancellationToken)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null)
        {
            return false;
        }

        // Check if user can revoke: either the link creator or the video owner
        var canRevoke = link.SharedBy == userId || link.Video.UploadedBy == userId;

        if (!canRevoke)
        {
            return false;
        }

        link.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<SharedLink>> GetUserSharedLinksAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var links = await dbContext.SharedLinks
            .Include(l => l.Video)
            .Where(l => l.SharedBy == userId || l.Video.UploadedBy == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.AsReadOnly();
    }

    public async Task<SharedLink?> GetSharedLinkAsync(string linkId, CancellationToken cancellationToken)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null)
        {
            return null;
        }

        // Validate link is not expired and not revoked
        if (link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            return null;
        }

        return link;
    }

    private string GenerateUniqueLinkId()
    {
        return ShortLinkGenerator.GenerateShortLinkId();
    }

    private async Task<bool> CanUserShareVideoAsync(Guid videoId, string userId, CancellationToken cancellationToken)
    {
        // Check if user is video owner
        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
        {
            return false;
        }

        if (video.UploadedBy == userId)
        {
            return true;
        }

        // Use AccessService to check if user has access to the video (includes group/event access)
        return await accessService.DoesUserHasAccessAsync(videoId, userId, cancellationToken);
    }
}
