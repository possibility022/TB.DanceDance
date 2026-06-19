using Application.Extensions;
using Application.Features.AccessManagement;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace Application.Features.Comments;

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
    
    
    // A comment thread is keyed off either a single video or a whole competition. These helpers let
    // the same visibility logic serve both kinds of thread.
    private static Expression<Func<Comment, bool>> ThreadPredicate(Guid? videoId, Guid? competitionId) =>
        videoId is not null
            ? c => c.VideoId == videoId
            : c => c.CompetitionId == competitionId;

    // Video/Competition are included explicitly (not left to incidental EF change-tracker fixup) so
    // CommentMapper.MapToResponse can always resolve the thread owner for CanModerate/IsReported.
    private IQueryable<Comment> QueryAllComments(Guid? videoId, Guid? competitionId) =>
        dbContext.Comments
            .Include(c => c.User)
            .Include(c => c.Video)
            .Include(c => c.Competition)
            .Where(ThreadPredicate(videoId, competitionId))
            .OrderBy(c => c.CreatedAt);
    

    public async Task<Comment> CreateCommentAsync(
        string? userId,
        string linkId,
        string content,
        string? authorName,
        string? anonymousId,
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

        // Get the shared link with its target (video or competition)
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .Include(l => l.Competition)
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

        // The link targets either a single video or a whole competition (combined thread).
        if (link.VideoId is null && link.CompetitionId is null)
        {
            throw new ArgumentException("This shared link does not target a video or competition.", nameof(linkId));
        }

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
        
        // When posted as an authorized user, do not store anonymous id
        byte[]? hashOfAnonymousId = null;
        if (string.IsNullOrEmpty(userId) && !string.IsNullOrWhiteSpace(anonymousId))
            hashOfAnonymousId = SHA256.HashData(Encoding.UTF8.GetBytes(anonymousId));

        // Create the comment, keyed off the link's target (video or competition).
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            VideoId = link.VideoId,
            CompetitionId = link.CompetitionId,
            UserId = userId, // null for anonymous, populated for authenticated
            SharedLinkId = linkId,
            Content = content,
            AnonymousName = authorName,
            ShaOfAnonymousId = hashOfAnonymousId,
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

    public async Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForVideoAsync(
        string? userId,
        string? anonymousId,
        string linkId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Validate the shared link exists and is valid
        var link = await dbContext.SharedLinks
            .Include(l => l.Video)
            .Include(l => l.Competition)
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken);

        if (link == null || link.ExpireAt <= DateTimeOffset.UtcNow || link.IsRevoked)
        {
            return (Array.Empty<Comment>(), 0);
        }

        // Resolve the thread's owner + visibility from the link's target (video or competition).
        if (link.Video != null)
        {
            return await QueryCommentsBase(userId, anonymousId, link.Video.OwnerUserId,
                link.Video.CommentVisibility, link.Video.Id, null, pageNumber, pageSize, cancellationToken);
        }

        if (link.Competition != null)
        {
            return await QueryCommentsBase(userId, anonymousId, link.Competition.OwnerUserId,
                link.Competition.CommentVisibility, null, link.Competition.Id, pageNumber, pageSize, cancellationToken);
        }

        return (Array.Empty<Comment>(), 0);
    }

    private async Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> QueryCommentsBase(
        string? userId,
        string? anonymousId,
        string ownerUserId,
        CommentVisibility commentVisibility,
        Guid? videoId,
        Guid? competitionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Check if user is the thread owner (video owner or competition owner)
        var isOwner = userId != null && ownerUserId == userId;

        // The owner always sees all comments (including hidden ones)
        if (isOwner)
        {
            return await QueryAllComments(videoId, competitionId)
                .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
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

        // Apply visibility rules based on the thread's CommentVisibility setting
        switch (commentVisibility)
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
                return (Array.Empty<Comment>(), 0);
        }

        // Get non-hidden comments
        return await baseQuery!
            .Where(ThreadPredicate(videoId, competitionId))
            .OrderBy(c => c.CreatedAt)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForVideoAsync(
        string userId,
        string? anonymousId,
        Guid videoId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(videoId, userId, cancellationToken);
        if (!hasAccess)
            throw new UnauthorizedAccessException("No access to the video.");

        var video = await dbContext.Videos.FirstAsync(v => v.Id == videoId, cancellationToken);

        return await QueryCommentsBase(userId, anonymousId, video.OwnerUserId, video.CommentVisibility,
            video.Id, null, pageNumber, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForCompetitionAsync(
        string userId,
        Guid competitionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var competition = await dbContext.Competitions
            .FirstOrDefaultAsync(c => c.Id == competitionId, cancellationToken);

        if (competition == null)
        {
            throw new ArgumentException($"Competition {competitionId} was not found.", nameof(competitionId));
        }

        if (competition.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("No access to the competition.");
        }

        // Always the owner branch of QueryCommentsBase, so this returns the full thread (incl. hidden).
        return await QueryCommentsBase(userId, anonymousId: null, competition.OwnerUserId,
            competition.CommentVisibility, videoId: null, competitionId: competition.Id,
            pageNumber, pageSize, cancellationToken);
    }

    public async Task<bool> UpdateCommentAsync(
        Guid commentId,
        string? userId,
        string? anonymousId,
        string? authorName,
        string content,
        CancellationToken cancellationToken)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        }
        
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(anonymousId))
            throw new ArgumentException("Either userId or anonymousId must be provided.", nameof(userId));

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
        
        bool shaMatches = CheckAnonymousIdMatch(anonymousId, comment);

        // Authenticated comment authors can update their own comments
        // or anonymous users that provided the same anonymous id
        if (comment.UserId == userId || shaMatches)
        {
            comment.Content = content;
            comment.UpdatedAt = DateTimeOffset.UtcNow;
            if (shaMatches && !string.IsNullOrWhiteSpace(authorName))
                comment.AnonymousName = authorName;
            
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// The owner of a comment's thread: the video owner for a per-video comment, or the competition
    /// owner for a competition (combined-thread) comment. Requires the Video/Competition nav loaded.
    /// </summary>
    private static string? ThreadOwnerOf(Comment comment) =>
        comment.Video?.OwnerUserId ?? comment.Competition?.OwnerUserId;

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

    public async Task<bool> DeleteCommentAsync(
        Guid commentId,
        string? userId,
        string? anonymousId,
        CancellationToken cancellationToken)
    {
        
        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(anonymousId))
            throw new ArgumentException("Either userId or anonymousId must be provided.", nameof(userId));
        
        var comment = await dbContext.Comments
            .Include(c => c.Video)
            .Include(c => c.Competition)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Check if user can delete: the comment author or the thread owner (video/competition owner)
        var isAuthor = !string.IsNullOrEmpty(userId) && comment.UserId == userId;
        var isThreadOwner = userId != null && ThreadOwnerOf(comment) == userId;
        bool shaMatches = CheckAnonymousIdMatch(anonymousId, comment);

        if (!isAuthor && !isThreadOwner && !shaMatches)
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
            .Include(c => c.Competition)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only the thread owner (video/competition owner) can hide comments
        if (ThreadOwnerOf(comment) != videoOwnerId)
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
            .Include(c => c.Competition)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
        {
            return false;
        }

        // Only the thread owner (video/competition owner) can unhide comments
        if (ThreadOwnerOf(comment) != videoOwnerId)
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
