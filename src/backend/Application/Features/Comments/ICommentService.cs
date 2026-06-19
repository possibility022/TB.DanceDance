using Domain.Entities;

namespace Application.Features.Comments;

public interface ICommentService
{
    /// <summary>
    /// Creates a comment on a video through a shared link.
    /// The video ID is resolved from the shared link.
    /// </summary>
    /// <param name="userId">The ID of the authenticated user creating the comment (null for anonymous)</param>
    /// <param name="linkId">The shared link ID through which the comment is being created</param>
    /// <param name="content">The comment content</param>
    /// <param name="authorName">The name of the author if posting as anonymous (null for authenticated users)</param>
    /// <param name="anonymousId">Anonymous user ID, used to identify the comment owner when comment was posted by/as anonymous user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created Comment</returns>
    /// <exception cref="ArgumentException">If link doesn't exist, doesn't allow comments, or other validation fails</exception>
    Task<Comment> CreateCommentAsync(string? userId,
        string linkId,
        string content,
        string? authorName,
        string? anonymousId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a page of comments for a video accessed through a shared link.
    /// Applies visibility rules based on video's CommentVisibility setting and user authentication status.
    /// The video ID is resolved from the shared link.
    /// </summary>
    /// <param name="userId">The ID of the user viewing comments (null for anonymous)</param>
    /// <param name="anonymousId">Anonymous user ID (null for non-anonymous users)</param>
    /// <param name="linkId">The shared link being used to access the video</param>
    /// <param name="pageNumber">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of comments per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page of visible comments and the total count of visible comments</returns>
    Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForVideoAsync(string? userId,
        string? anonymousId,
        string linkId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a page of comments for a specific video.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the comments</param>
    /// <param name="videoId">The unique identifier of the video blob</param>
    /// <param name="anonymousId">Anonymous id to provide to get comments posted as anonymous user.</param>
    /// <param name="pageNumber">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of comments per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page of comments for the specified video and the total count of visible comments</returns>
    /// <exception cref="ArgumentException">If the video does not exist or validation fails</exception>
    /// <exception cref="UnauthorizedAccessException">If the user is not authorized to access the video's comments</exception>
    Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForVideoAsync(
        string userId,
        string? anonymousId,
        Guid videoId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a page of comments for the combined thread of a competition, directly by id
    /// (not via a shared link). Owner-only — a competition has no group/event-style access, so
    /// unlike the video overload this always returns the full (incl. hidden) thread.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the comments</param>
    /// <param name="competitionId">The competition id</param>
    /// <param name="pageNumber">The 1-based page number to retrieve</param>
    /// <param name="pageSize">The number of comments per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The page of comments for the competition's combined thread and the total count</returns>
    /// <exception cref="ArgumentException">If the competition does not exist</exception>
    /// <exception cref="UnauthorizedAccessException">If the user is not the competition's owner</exception>
    Task<(IReadOnlyCollection<Comment> Items, int TotalCount)> GetCommentsForCompetitionAsync(
        string userId,
        Guid competitionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates a comment. Only the authenticated comment author can update their own comments.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="userId">The ID of the user updating the comment</param>
    /// <param name="anonymousId"></param>
    /// <param name="authorName">The name of the author if posting as anonymous (null for authenticated users)</param>
    /// <param name="content">The new comment content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found or unauthorized</returns>
    Task<bool> UpdateCommentAsync(Guid commentId,
        string? userId,
        string? anonymousId,
        string? authorName,
        string content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a comment. Can be deleted by the comment author (if authenticated) or the video owner.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="userId">The ID of the user deleting the comment</param>
    /// <param name="anonymousId">Anonymous id that is stored on the client side. Allows deleting comments posted anonymously</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteCommentAsync(
        Guid commentId,
        string? userId,
        string? anonymousId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Hides a comment. Only the video owner can hide comments.
    /// Hidden comments are only visible to the video owner.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="videoOwnerId">The ID of the video owner</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if hidden successfully, false if not found or unauthorized</returns>
    Task<bool> HideCommentAsync(
        Guid commentId,
        string videoOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unhides a comment. Only the video owner can unhide comments.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="videoOwnerId">The ID of the video owner</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if unhidden successfully, false if not found or unauthorized</returns>
    Task<bool> UnhideCommentAsync(
        Guid commentId,
        string videoOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reports a comment as inappropriate. Can be called by anyone (authenticated or anonymous).
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="reason">The reason for reporting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if reported successfully, false if not found</returns>
    Task<bool> ReportCommentAsync(
        Guid commentId,
        string reason,
        CancellationToken cancellationToken);
}
