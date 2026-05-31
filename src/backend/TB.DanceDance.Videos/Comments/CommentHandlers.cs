using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Comments;

/// <summary>
/// Comments feature, ported verbatim from the old <c>CommentService</c>. Everything is local to
/// <see cref="VideosDbContext"/> (Comments + SharedLinks + Videos); the single cross-module concern
/// is the access check in <see cref="GetCommentsForVideoByIdQuery"/>, which reuses Task 01's
/// <see cref="DoesUserHaveAccessToVideoQuery"/> through the mediator.
/// </summary>
class CommentHandlers
    : IRequestHandler<CreateCommentCommand, CommentDto>,
      IRequestHandler<GetCommentsForVideoByLinkQuery, IReadOnlyCollection<CommentDto>>,
      IRequestHandler<GetCommentsForVideoByIdQuery, IReadOnlyCollection<CommentDto>>,
      IRequestHandler<UpdateCommentCommand, bool>,
      IRequestHandler<DeleteCommentCommand, bool>,
      IRequestHandler<HideCommentCommand, bool>,
      IRequestHandler<UnhideCommentCommand, bool>,
      IRequestHandler<ReportCommentCommand, bool>
{
    private const int MaxCommentLength = 2000;

    private readonly VideosDbContext dbContext;
    private readonly IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQueryHandler;

    public CommentHandlers(VideosDbContext dbContext, IRequestHandler<DoesUserHaveAccessToVideoQuery, bool> doesUserHaveAccessToVideoQueryHandler)
    {
        this.dbContext = dbContext;
        this.doesUserHaveAccessToVideoQueryHandler = doesUserHaveAccessToVideoQueryHandler;
    }

    public async Task<CommentDto> HandleAsync(CreateCommentCommand request, CancellationToken cancellationToken = default)
    {
        var content = request.Content;
        var userId = request.UserId;

        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty.", nameof(request.Content));
        }

        if (content.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxCommentLength} characters.",
                nameof(request.Content));
        }

        if (string.IsNullOrWhiteSpace(request.AuthorName) && string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Either userId or authorName must be provided for comments.", nameof(request.AuthorName));

        // Get the shared link with video info
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == request.LinkId, cancellationToken);

        if (link == null)
        {
            throw new ArgumentException("Shared link not found.", nameof(request.LinkId));
        }

        // Validate link is not expired and not revoked
        if (link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            throw new ArgumentException("Shared link is expired or revoked.", nameof(request.LinkId));
        }

        // Get videoId from the link
        var videoId = link.VideoId;

        // Check if comments are allowed on this link
        if (!link.AllowComments)
        {
            throw new ArgumentException("Comments are not allowed through this shared link.", nameof(request.LinkId));
        }

        // If user is authenticated, they must comment as themselves (cannot comment anonymously)
        // If user is anonymous, check if anonymous comments are allowed
        if (userId == null && !link.AllowAnonymousComments)
        {
            throw new ArgumentException(
                "Anonymous comments are not allowed through this shared link. Please log in to comment.",
                nameof(request.UserId));
        }

        // When posted as an authorized user, do not store anonymous id
        byte[]? hashOfAnonymousId = null;
        if (string.IsNullOrEmpty(userId) && !string.IsNullOrWhiteSpace(request.AnonymousId))
            hashOfAnonymousId = SHA256.HashData(Encoding.UTF8.GetBytes(request.AnonymousId));

        // Create the comment
        var comment = Comment.Factory.Create(
            videoId,
            userId,
            request.LinkId,
            content,
            request.AuthorName,
            hashOfAnonymousId);

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapComment(comment, link.Video.UploadedBy);
    }

    public async Task<IReadOnlyCollection<CommentDto>> HandleAsync(GetCommentsForVideoByLinkQuery request, CancellationToken cancellationToken = default)
    {
        // Validate the shared link exists and is valid
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == request.LinkId, cancellationToken);

        if (link == null || link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            return Array.Empty<CommentDto>();
        }

        return await QueryCommentsBase(request.UserId, request.AnonymousId, link.Video, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CommentDto>> HandleAsync(GetCommentsForVideoByIdQuery request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await doesUserHaveAccessToVideoQueryHandler.HandleAsync(
            new DoesUserHaveAccessToVideoQuery(request.UserId, request.VideoId), cancellationToken);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to the video.");

        var video = await dbContext.Videos.FirstAsync(v => v.Id == request.VideoId, cancellationToken);

        return await QueryCommentsBase(request.UserId, request.AnonymousId, video, cancellationToken);
    }

    private async Task<IReadOnlyCollection<CommentDto>> QueryCommentsBase(string? userId, string? anonymousId, Video video, CancellationToken cancellationToken)
    {
        // Check if user is the video owner
        var isVideoOwner = userId != null && video.UploadedBy == userId;
        var videoId = video.Id;

        // Video owner always sees all comments (including hidden ones)
        if (isVideoOwner)
        {
            var ownerComments = await dbContext.Comments
                .Where(c => c.VideoId == videoId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return ownerComments.Select(c => MapComment(c, video.UploadedBy)).ToList().AsReadOnly();
        }

        Expression<Func<Comment, bool>> predicate = c => false;
        IQueryable<Comment>? baseQuery = null;

        if (anonymousId is not null)
        {
            var hashedId = SHA256.HashData(Encoding.UTF8.GetBytes(anonymousId));
            predicate = predicate.Or(c => c.ShaOfAnonymousId == hashedId);
        }

        if (userId is not null)
        {
            predicate = predicate.Or(c => c.UserId == userId);
        }

        // Apply visibility rules based on video's CommentVisibility setting
        switch (video.CommentVisibility)
        {
            case CommentVisibility.OwnerOnly:
                // Only owner can see comments (and we already handled that case above)
                // Here we are adding only those comments that were posted by given user.
                // Authenticated or with specific anonymous id.
                baseQuery = dbContext.Comments.Where(predicate);
                break;

            case CommentVisibility.AuthenticatedOnly:
                if (userId is not null)
                {
                    predicate = predicate.Or(c => c.IsHidden == false);
                }
                baseQuery = dbContext.Comments.Where(predicate);
                break;

            case CommentVisibility.Public:
                predicate = c => !c.IsHidden; // query all not hidden comments
                baseQuery = dbContext.Comments.Where(predicate);
                break;

            default:
                return Array.Empty<CommentDto>();
        }

        // Get the visible comments
        var comments = await baseQuery!
            .Where(c => c.VideoId == videoId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(c => MapComment(c, video.UploadedBy)).ToList().AsReadOnly();
    }

    public async Task<bool> HandleAsync(UpdateCommentCommand request, CancellationToken cancellationToken = default)
    {
        var content = request.Content;

        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty.", nameof(request.Content));
        }

        if (string.IsNullOrWhiteSpace(request.UserId) && string.IsNullOrWhiteSpace(request.AnonymousId))
            throw new ArgumentException("Either userId or anonymousId must be provided.", nameof(request.UserId));

        if (content.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxCommentLength} characters.",
                nameof(request.Content));
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        bool shaMatches = CheckAnonymousIdMatch(request.AnonymousId, comment);

        // Authenticated comment authors can update their own comments
        // or anonymous users that provided the same anonymous id
        if (comment.UserId == request.UserId || shaMatches)
        {
            comment.Content = content;
            comment.UpdatedAt = DateTimeOffset.UtcNow;
            if (shaMatches && !string.IsNullOrWhiteSpace(request.AuthorName))
                comment.AnonymousName = request.AuthorName;

            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }

    private static bool CheckAnonymousIdMatch(string? anonymousId, Comment comment)
    {
        byte[]? anonymousIdBytes = null;
        if (!string.IsNullOrWhiteSpace(anonymousId))
            anonymousIdBytes = SHA256.HashData(Encoding.UTF8.GetBytes(anonymousId));

        var shaMatches = anonymousIdBytes is not null
                         && comment.ShaOfAnonymousId is not null
                         && anonymousIdBytes.Length > 0
                         && comment.ShaOfAnonymousId.Length > 0
                         && anonymousIdBytes.SequenceCompareTo(comment.ShaOfAnonymousId) == 0;
        return shaMatches;
    }

    public async Task<bool> HandleAsync(DeleteCommentCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) && string.IsNullOrWhiteSpace(request.AnonymousId))
            throw new ArgumentException("Either userId or anonymousId must be provided.", nameof(request.UserId));

        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Check if user can delete: either the comment author or the video owner
        var isAuthor = !string.IsNullOrEmpty(request.UserId) && comment.UserId == request.UserId;
        var isVideoOwner = comment.Video.UploadedBy == request.UserId;
        bool shaMatches = CheckAnonymousIdMatch(request.AnonymousId, comment);

        if (!isAuthor && !isVideoOwner && !shaMatches)
        {
            return false;
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HandleAsync(HideCommentCommand request, CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only video owner can hide comments
        if (comment.Video.UploadedBy != request.VideoOwnerId)
        {
            return false;
        }

        comment.IsHidden = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HandleAsync(UnhideCommentCommand request, CancellationToken cancellationToken = default)
    {
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only video owner can unhide comments
        if (comment.Video.UploadedBy != request.VideoOwnerId)
        {
            return false;
        }

        comment.IsHidden = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HandleAsync(ReportCommentCommand request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Report reason cannot be empty.", nameof(request.Reason));
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        comment.IsReported = true;
        comment.ReportedReason = request.Reason;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CommentDto MapComment(Comment comment, string videoOwnerId) => new()
    {
        Id = comment.Id,
        VideoId = comment.VideoId,
        UserId = comment.UserId,
        Content = comment.Content,
        PostedAsAnonymous = comment.PostedAsAnonymous,
        AnonymousName = comment.AnonymousName,
        ShaOfAnonymousId = comment.ShaOfAnonymousId,
        CreatedAt = comment.CreatedAt,
        UpdatedAt = comment.UpdatedAt,
        IsHidden = comment.IsHidden,
        IsReported = comment.IsReported,
        ReportedReason = comment.ReportedReason,
        VideoOwnerId = videoOwnerId,
    };
}
