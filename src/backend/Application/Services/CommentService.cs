using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class CommentService : ICommentService
{
    private readonly IApplicationContext dbContext;
    private readonly IAccessService accessService;
    private const int MaxCommentLength = 2000;

    public CommentService(IApplicationContext dbContext, IAccessService accessService)
    {
        this.dbContext = dbContext;
        this.accessService = accessService;
    }

    public async Task<Comment> CreateCommentAsync(
        string? userId,
        string linkId,
        string content,
        string? authorName,
        CancellationToken cancellationToken)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        }

        if (content.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxCommentLength} characters.",
                nameof(content));
        }
        
        if (string.IsNullOrWhiteSpace(authorName) && string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("Either userId or authorName must be provided for comments.", nameof(authorName));

        // Get the shared link with video info
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null)
        {
            throw new ArgumentException("Shared link not found.", nameof(linkId));
        }

        // Validate link is not expired and not revoked
        if (link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            throw new ArgumentException("Shared link is expired or revoked.", nameof(linkId));
        }

        // Get videoId from the link
        var videoId = link.VideoId;

        // Check if comments are allowed on this link
        if (!link.AllowComments)
        {
            throw new ArgumentException("Comments are not allowed through this shared link.", nameof(linkId));
        }

        // If user is authenticated, they must comment as themselves (cannot comment anonymously)
        // If user is anonymous, check if anonymous comments are allowed
        if (userId == null && !link.AllowAnonymousComments)
        {
            throw new ArgumentException(
                "Anonymous comments are not allowed through this shared link. Please log in to comment.",
                nameof(userId));
        }

        // Create the comment
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            VideoId = videoId,
            UserId = userId, // null for anonymous, populated for authenticated
            SharedLinkId = linkId,
            Content = content,
            AnonymouseName = authorName,
            PostedAsAnonymous = userId is null, 
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            IsHidden = false,
            IsReported = false,
            ReportedReason = null
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return comment;
    }

    public async Task<IReadOnlyCollection<Comment>> GetCommentsForVideoAsync(
        string? userId,
        string linkId,
        CancellationToken cancellationToken)
    {
        // Validate the shared link exists and is valid
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null || link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            return Array.Empty<Comment>();
        }

        var videoId = link.VideoId;
        var video = link.Video;

        // Check if user is the video owner
        var isVideoOwner = userId != null && video.UploadedBy == userId;

        // Video owner always sees all comments (including hidden ones)
        if (isVideoOwner)
        {
            var ownerComments = await dbContext.Comments
                .Include(c => c.User)
                .Include(c => c.SharedLink)
                .Where(c => c.VideoId == videoId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            return ownerComments.AsReadOnly();
        }

        // Apply visibility rules based on video's CommentVisibility setting
        switch (video.CommentVisibility)
        {
            case CommentVisibility.OwnerOnly:
                // Only owner can see comments (and we already handled that case above)
                return Array.Empty<Comment>();

            case CommentVisibility.AuthenticatedOnly:
                // Only authenticated users can see comments
                if (userId == null)
                {
                    return Array.Empty<Comment>();
                }
                break;

            case CommentVisibility.Public:
                // Everyone can see comments (including anonymous users)
                break;

            default:
                return Array.Empty<Comment>();
        }

        // Get non-hidden comments
        var comments = await dbContext.Comments
            .Include(c => c.User)
            .Include(c => c.SharedLink)
            .Where(c => c.VideoId == videoId && !c.IsHidden)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Comment>> GetCommentsForVideoAsync(string userId, Guid videoId, CancellationToken cancellationToken)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(videoId, userId, cancellationToken);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to the video.");

        var hiddenComments = dbContext.Comments
            .Include(c => c.User)
            .Where(c => c.VideoId == videoId && c.IsHidden)
            .Where(c => c.Video.UploadedBy == userId);

        var comments = dbContext.Comments
            .Include(c => c.User)
            .Where(c => c.VideoId == videoId && !c.IsHidden)
            .OrderBy(c => c.CreatedAt);

        var allComments = await hiddenComments
            .Union(comments)
            .Distinct()
            .ToListAsync(cancellationToken);

        return allComments.AsReadOnly();
    }

    public async Task<bool> UpdateCommentAsync(
        Guid commentId,
        string userId,
        string content,
        CancellationToken cancellationToken)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        }

        if (content.Length > MaxCommentLength)
        {
            throw new ArgumentException(
                $"Comment content cannot exceed {MaxCommentLength} characters.",
                nameof(content));
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only authenticated comment authors can update their own comments
        if (comment.UserId != userId)
        {
            return false;
        }

        comment.Content = content;
        comment.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteCommentAsync(
        Guid commentId,
        string userId,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Check if user can delete: either the comment author or the video owner
        var isAuthor = comment.UserId == userId;
        var isVideoOwner = comment.Video.UploadedBy == userId;

        if (!isAuthor && !isVideoOwner)
        {
            return false;
        }

        dbContext.Comments.Remove(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HideCommentAsync(
        Guid commentId,
        string videoOwnerId,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only video owner can hide comments
        if (comment.Video.UploadedBy != videoOwnerId)
        {
            return false;
        }

        comment.IsHidden = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnhideCommentAsync(
        Guid commentId,
        string videoOwnerId,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only video owner can unhide comments
        if (comment.Video.UploadedBy != videoOwnerId)
        {
            return false;
        }

        comment.IsHidden = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReportCommentAsync(
        Guid commentId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Report reason cannot be empty.", nameof(reason));
        }

        var comment = await dbContext.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        comment.IsReported = true;
        comment.ReportedReason = reason;

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
