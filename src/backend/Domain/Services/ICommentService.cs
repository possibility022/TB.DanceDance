using Domain.Entities;

namespace Domain.Services;

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
    /// <param name="anonymouseId">Anonymous user ID, used to identify the comment owner when comment was posted by/as anonymouse user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created Comment</returns>
    /// <exception cref="ArgumentException">If link doesn't exist, doesn't allow comments, or other validation fails</exception>
    Task<Comment> CreateCommentAsync(string? userId,
        string linkId,
        string content,
        string? authorName,
        string? anonymouseId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets comments for a video accessed through a shared link.
    /// Applies visibility rules based on video's CommentVisibility setting and user authentication status.
    /// The video ID is resolved from the shared link.
    /// </summary>
    /// <param name="userId">The ID of the user viewing comments (null for anonymous)</param>
    /// <param name="anonymouseId">Anonymous user ID (null for non-anonymous users)</param>
    /// <param name="linkId">The shared link being used to access the video</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of visible comments</returns>
    Task<IReadOnlyCollection<Comment>> GetCommentsForVideoAsync(string? userId,
        string? anonymouseId,
        string linkId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a collection of comments for a specific video.
    /// </summary>
    /// <param name="userId">The ID of the user requesting the comments</param>
    /// <param name="videoId">The unique identifier of the video blob</param>
    /// <param name="anonymouseId">Anonymouse id to provide to get comments posted as anonymouse user.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only collection of comments for the specified video</returns>
    /// <exception cref="ArgumentException">If the video does not exist or validation fails</exception>
    /// <exception cref="UnauthorizedAccessException">If the user is not authorized to access the video's comments</exception>
    Task<IReadOnlyCollection<Comment>> GetCommentsForVideoAsync(
        string userId,
        string? anonymouseId,
        Guid videoId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates a comment. Only the authenticated comment author can update their own comments.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="userId">The ID of the user updating the comment</param>
    /// <param name="anonymouseId"></param>
    /// <param name="content">The new comment content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully, false if not found or unauthorized</returns>
    Task<bool> UpdateCommentAsync(Guid commentId,
        string? userId,
        string? anonymouseId,
        string content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a comment. Can be deleted by the comment author (if authenticated) or the video owner.
    /// </summary>
    /// <param name="commentId">The comment ID</param>
    /// <param name="userId">The ID of the user deleting the comment</param>
    /// <param name="anonymouseId">Anonymouse id that is stored on the client side. Allows deleting comments posted anonymously</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if not found or unauthorized</returns>
    Task<bool> DeleteCommentAsync(
        Guid commentId,
        string? userId,
        string? anonymouseId,
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
