using Domain.Entities;
using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class CommentsController : Controller
{
    private readonly ICommentService commentService;
    private readonly ILogger<CommentsController> logger;
    public const string AnonymousHeaderId = "AnonymousId";

    public CommentsController(ICommentService commentService, ILogger<CommentsController> logger)
    {
        this.commentService = commentService;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a comment on a video through a shared link. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [Route(ApiEndpoints.Comments.Create)]
    public async Task<ActionResult<CommentResponse>> CreateComment(
        [FromRoute] string linkId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        // Get userId if user is authenticated, null if anonymous
        var userId = User.Identity?.IsAuthenticated == true ? User.GetSubject() : null;

        try
        {
            var comment = await commentService.CreateCommentAsync(
                userId,
                linkId,
                request.Content,
                request.AuthorName, 
                request.AnonymousId,
                cancellationToken);
            
            var response = MapToResponse(comment, userId, null);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to create comment through link {LinkId}", linkId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets comments for a video accessed through a shared link. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route(ApiEndpoints.Comments.GetByLink)]
    public async Task<ActionResult<IEnumerable<CommentResponse>>> GetCommentsByLink(
        [FromRoute] string linkId,
        [FromQuery] string? anonymouseId,
        CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetSubject() : null;
        anonymouseId = ResolveAnonymousId(anonymouseId, Request);
        
        try
        {
            var comments = await commentService.GetCommentsForVideoAsync(
                userId,
                anonymouseId,
                linkId,
                cancellationToken);

            byte[]? shaOfAnonymousId = null;
            if (anonymouseId != null)
                shaOfAnonymousId = SHA256.HashData(Encoding.UTF8.GetBytes(anonymouseId));

            var response = comments.Select(c => MapToResponse(c, userId, shaOfAnonymousId));
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to get comments for link {LinkId}", linkId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a comment. Only the authenticated comment author can update.
    /// </summary>
    [HttpPut]
    [Route(ApiEndpoints.Comments.Update)]
    public async Task<IActionResult> UpdateComment(
        [FromRoute] Guid commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        try
        {
            var result = await commentService.UpdateCommentAsync(commentId,
                userId,
                request.AnonymousId,
                request.Content,
                cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Comment not found or you are not authorized to update it." });
            }

            return Ok();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to update comment {CommentId}", commentId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    private string? ResolveAnonymousId(string? anonymouseIdFromQuery, HttpRequest request)
    {
        if (!string.IsNullOrEmpty(anonymouseIdFromQuery))
            return anonymouseIdFromQuery;
        
        string? anonymouseIdFromHeader = request.Headers[AnonymousHeaderId].FirstOrDefault();
        return  anonymouseIdFromHeader;
    }

    /// <summary>
    /// Deletes a comment. Can be deleted by the author or video owner.
    /// </summary>
    [AllowAnonymous]
    [Route(ApiEndpoints.Comments.Delete)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] Guid commentId,
        [FromQuery] string? anonymouseId,
        CancellationToken cancellationToken)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetSubject() : null;

        anonymouseId = ResolveAnonymousId(anonymouseId, Request);

        try
        {
            var result = await commentService.DeleteCommentAsync(commentId,
                userId,
                anonymouseId,
                cancellationToken);
            
            if (!result)
            {
                return NotFound(new { error = "Comment not found or you are not authorized to delete it." });
            }
        }
        catch (ArgumentException e)
        {
            return BadRequest(new { error = e.Message });
        }
        
        return Ok();
    }

    /// <summary>
    /// Hides a comment. Only the video owner can hide comments.
    /// </summary>
    [HttpPut]
    [Route(ApiEndpoints.Comments.Hide)]
    public async Task<IActionResult> HideComment(
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var result = await commentService.HideCommentAsync(commentId, userId, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Comment not found or you are not authorized to hide it." });
        }

        return Ok();
    }

    /// <summary>
    /// Unhides a comment. Only the video owner can unhide comments.
    /// </summary>
    [HttpPut]
    [Route(ApiEndpoints.Comments.Unhide)]
    public async Task<IActionResult> UnhideComment(
        [FromRoute] Guid commentId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var result = await commentService.UnhideCommentAsync(commentId, userId, cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Comment not found or you are not authorized to unhide it." });
        }

        return Ok();
    }

    /// <summary>
    /// Reports a comment as inappropriate. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [Route(ApiEndpoints.Comments.Report)]
    public async Task<IActionResult> ReportComment(
        [FromRoute] Guid commentId,
        [FromBody] ReportCommentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await commentService.ReportCommentAsync(commentId, request.Reason, cancellationToken);

            if (!result)
            {
                return NotFound(new { error = "Comment not found." });
            }

            return Ok();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to report comment {CommentId}", commentId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet]
    [Route(ApiEndpoints.Comments.GetCommentsForVideo)]
    public async Task<IActionResult> GetCommentsForVideo([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();
        string? anonymouseId = null;
            
        try
        {
            anonymouseId = ResolveAnonymousId(anonymouseId, Request);
            
            var comments = await commentService.GetCommentsForVideoAsync(userId, anonymouseId, videoId, cancellationToken);
            
            byte[]? shaOfAnonymousId = null;
            if (anonymouseId != null)
                shaOfAnonymousId = SHA256.HashData(Encoding.UTF8.GetBytes(anonymouseId));
            
            return Ok(comments.Select(c => MapToResponse(c, userId, shaOfAnonymousId)));
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Failed to get comments for video {VideoId}. User unauthorized", videoId);
            return Unauthorized();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to get comments for video {VideoId}", videoId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private CommentResponse MapToResponse(Comment comment, string? currentUserId, byte[]? anonymouseId)
    {
        var isVideoOwner = comment.Video?.UploadedBy == currentUserId;
        var isAuthor = comment.UserId == currentUserId && currentUserId != null;
        var isAnonymousAuthor = comment.ShaOfAnonymousId?.SequenceEqual(anonymouseId) ?? false;
        
        string? authorName;
        if (comment.PostedAsAnonymous)
            authorName = comment.AnonymousName;
        else
            authorName = comment.User != null ? $"{comment.User.FirstName} {comment.User.LastName}" : null;

        return new CommentResponse
        {
            Id = comment.Id,
            VideoId = comment.VideoId,
            AuthorName = authorName,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsHidden = comment.IsHidden,
            PostedAsAnonymous = comment.PostedAsAnonymous,
            // Only populate moderation fields for video owner
            IsReported = isVideoOwner ? comment.IsReported : null,
            ReportedReason = isVideoOwner ? comment.ReportedReason : null,
            IsOwn = isAuthor || isAnonymousAuthor,
            CanModerate = isVideoOwner
        };
    }
}
