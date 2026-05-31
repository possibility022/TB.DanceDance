using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Sharing;

/// <summary>
/// Public short-link (view-sharing) feature: create, resolve, revoke and list links.
/// Ported from the old <c>SharedLinkService</c>. The create-time authorization (owner OR
/// has-access) is the only cross-module concern and goes through the mediator via
/// <see cref="DoesUserHaveAccessToVideoQuery"/>; everything else is local to the Videos module.
/// </summary>
class SharedLinkHandlers
    : IRequestHandler<CreateSharedLinkCommand, SharedLinkDto>,
      IRequestHandler<GetVideoBySharedLinkQuery, VideoDto?>,
      IRequestHandler<RevokeSharedLinkCommand, bool>,
      IRequestHandler<GetUserSharedLinksQuery, IReadOnlyCollection<SharedLinkDto>>,
      IRequestHandler<GetSharedLinkQuery, SharedLinkDto?>
{
    private const int MaxRetries = 5;
    private const int MinExpirationDays = 1;
    private const int MaxExpirationDays = 365;

    private readonly VideosDbContext dbContext;
    private readonly IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQueryHandler;

    public SharedLinkHandlers(VideosDbContext dbContext, IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQueryHandler)
    {
        this.dbContext = dbContext;
        this.doesUserHaveAccessToVideoQueryHandler = doesUserHaveAccessToVideoQueryHandler;
    }

    public async Task<SharedLinkDto> HandleAsync(CreateSharedLinkCommand request, CancellationToken cancellationToken = default)
    {
        // Validate expiration days
        if (request.ExpirationDays < MinExpirationDays || request.ExpirationDays > MaxExpirationDays)
        {
            throw new ArgumentException(
                $"Expiration days must be between {MinExpirationDays} and {MaxExpirationDays}.",
                nameof(request.ExpirationDays));
        }

        // Check if video exists
        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == request.VideoId, cancellationToken);

        if (video == null)
        {
            throw new ArgumentException($"Video with ID {request.VideoId} not found.", nameof(request.VideoId));
        }

        // Check if user can share this video
        var canShare = await CanUserShareVideoAsync(video, request.UserId, cancellationToken);
        if (!canShare)
        {
            throw new ArgumentException(
                $"User {request.UserId} is not authorized to share video {request.VideoId}.",
                nameof(request.UserId));
        }

        // Generate unique short link ID with collision retry
        string linkId = ShortLinkGenerator.GenerateShortLinkId();
        int retries = 0;

        while (await dbContext.SharedLinks.AnyAsync(l => l.Id == linkId, cancellationToken) && retries < MaxRetries)
        {
            linkId = ShortLinkGenerator.GenerateShortLinkId();
            retries++;
        }

        if (retries >= MaxRetries)
        {
            throw new InvalidOperationException("Failed to generate unique link ID after maximum retries.");
        }

        var sharedLink = SharedLink.Factory.Create(
            linkId,
            request.VideoId,
            request.UserId,
            request.ExpirationDays,
            request.AllowComments,
            request.AllowAnonymousComments);

        dbContext.SharedLinks.Add(sharedLink);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapLink(sharedLink, video: null);
    }

    public async Task<VideoDto?> HandleAsync(GetVideoBySharedLinkQuery request, CancellationToken cancellationToken = default)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == request.LinkId, cancellationToken);

        if (link == null || !IsActive(link))
        {
            return null;
        }

        return MapVideo(link.Video);
    }

    public async Task<bool> HandleAsync(RevokeSharedLinkCommand request, CancellationToken cancellationToken = default)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == request.LinkId, cancellationToken);

        if (link == null)
        {
            return false;
        }

        // Allowed for the link creator or the video owner
        var canRevoke = link.SharedBy == request.UserId || link.Video.UploadedBy == request.UserId;
        if (!canRevoke)
        {
            return false;
        }

        link.IsRevoked = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<SharedLinkDto>> HandleAsync(GetUserSharedLinksQuery request, CancellationToken cancellationToken = default)
    {
        var links = await dbContext.SharedLinks
            .Include(l => l.Video)
            .Where(l => l.SharedBy == request.UserId || l.Video.UploadedBy == request.UserId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.Select(l => MapLink(l, l.Video)).ToList().AsReadOnly();
    }

    public async Task<SharedLinkDto?> HandleAsync(GetSharedLinkQuery request, CancellationToken cancellationToken = default)
    {
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == request.LinkId, cancellationToken);

        if (link == null || !IsActive(link))
        {
            return null;
        }

        return MapLink(link, link.Video);
    }

    private async Task<bool> CanUserShareVideoAsync(Video video, string userId, CancellationToken cancellationToken)
    {
        // Owner check is local and short-circuits first.
        if (video.UploadedBy == userId)
        {
            return true;
        }

        // Otherwise delegate the (cross-module) access decision to the Videos access check,
        // which resolves group/event shares through the Access module.
        return await doesUserHaveAccessToVideoQueryHandler.HandleAsync(
            new DoesUserHaveAccessToVideoQuery(userId, video.Id), cancellationToken);
    }

    private static bool IsActive(SharedLink link)
        => link.ExpireAt > DateTimeOffset.UtcNow && !link.IsRevoked;

    private static SharedLinkDto MapLink(SharedLink link, Video? video) => new()
    {
        Id = link.Id,
        VideoId = link.VideoId,
        SharedBy = link.SharedBy,
        CreatedAt = link.CreatedAt,
        ExpireAt = link.ExpireAt,
        IsRevoked = link.IsRevoked,
        AllowComments = link.AllowComments,
        AllowAnonymousComments = link.AllowAnonymousComments,
        Video = video is null ? null : MapVideo(video),
    };

    private static VideoDto MapVideo(Video video) => new()
    {
        Id = video.Id,
        BlobId = video.BlobId!,
        Name = video.Name,
        RecordedDateTime = video.RecordedDateTime,
        Duration = video.Duration,
        Converted = video.Converted,
        CommentVisibility = (int)video.CommentVisibility,
    };
}
