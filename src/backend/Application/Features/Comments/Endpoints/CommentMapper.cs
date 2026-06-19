using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using CommentResponse = TB.DanceDance.API.Contracts.Features.Comments.CommentResponse;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Shared helpers for the comment endpoints: maps a <see cref="Comment"/> to its API contract and
/// resolves the anonymous-user identifier that authenticates anonymously-posted comments.
/// Ported from the former <c>CommentsController</c> private helpers.
/// </summary>
internal static class CommentMapper
{
    /// <summary>
    /// Header carrying the client-generated anonymous id (used when no <c>anonymousId</c> query value
    /// is supplied), allowing anonymous authors to edit/delete their own comments.
    /// </summary>
    public const string AnonymousHeaderId = "AnonymousId";

    /// <summary>
    /// Hashes the raw anonymous id so it can be compared against the stored <see cref="Comment.ShaOfAnonymousId"/>.
    /// Returns null when there is no anonymous id.
    /// </summary>
    public static byte[]? ComputeSha256(string? anonymousId)
        => anonymousId == null ? null : SHA256.HashData(Encoding.UTF8.GetBytes(anonymousId));

    /// <summary>
    /// Resolves the anonymous id, preferring the query-string value and falling back to the
    /// <see cref="AnonymousHeaderId"/> request header.
    /// </summary>
    public static string? ResolveAnonymousId(HttpRequest request)
    {
        var res = request.Query.TryGetValue("anonymousId", out var anonymousIdFromQueryQuery);
        
        if (res && anonymousIdFromQueryQuery.Count > 0)
        {
            var firstOne = anonymousIdFromQueryQuery.First();
            if (!string.IsNullOrEmpty(firstOne))
            {
                return firstOne;
            }
        }

        return request.Headers[AnonymousHeaderId].FirstOrDefault();
    }

    /// <summary>
    /// Projects a comment to its API response, computing the per-viewer flags (ownership, moderation).
    /// Moderation fields are only populated for the thread owner — the video owner for a per-video
    /// comment, or the competition owner for a combined-thread comment.
    /// </summary>
    /// <param name="comment">The comment entity (with its Video/Competition nav loaded).</param>
    /// <param name="currentUserId">The authenticated viewer's id, or null when anonymous.</param>
    /// <param name="anonymousId">SHA-256 of the viewer's anonymous id, used to detect anonymous authorship.</param>
    public static CommentResponse MapToResponse(Comment comment, string? currentUserId, byte[]? anonymousId)
    {
        var threadOwnerId = comment.Video?.OwnerUserId ?? comment.Competition?.OwnerUserId;
        var isThreadOwner = currentUserId != null && threadOwnerId == currentUserId;
        var isAuthor = comment.UserId == currentUserId && currentUserId != null;
        var isAnonymousAuthor = comment.ShaOfAnonymousId?.SequenceEqual(anonymousId) ?? false;

        string? authorName;
        if (comment.PostedAsAnonymous)
            authorName = comment.AnonymousName;
        else
            authorName = comment.User != null ? $"{comment.User.FirstName} {comment.User.LastName}" : null;

        return new CommentResponse
        {
            Id = comment.Id,
            // Guid.Empty for competition (combined-thread) comments, which have no single video.
            VideoId = comment.VideoId ?? System.Guid.Empty,
            AuthorName = authorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsHidden = comment.IsHidden,
            PostedAsAnonymous = comment.PostedAsAnonymous,
            // Only populate moderation fields for the thread owner
            IsReported = isThreadOwner ? comment.IsReported : null,
            ReportedReason = isThreadOwner ? comment.ReportedReason : null,
            IsOwn = isAuthor || isAnonymousAuthor,
            CanModerate = isThreadOwner
        };
    }
}