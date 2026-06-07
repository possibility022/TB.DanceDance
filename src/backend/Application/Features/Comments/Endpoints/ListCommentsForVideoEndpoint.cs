using Application.Extensions;
using Application.Pagination;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Comments.Endpoints;

public class ListCommentsForVideoRequest : PagedRequest
{
}

/// <summary>
/// Gets comments for a video the authenticated user has access to.
/// </summary>
public class ListCommentsForVideoEndpoint : Endpoint<ListCommentsForVideoRequest, PagedResponse<CommentResponse>>
{
    private readonly ICommentService commentService;

    public ListCommentsForVideoEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Comments.ListCommentsForVideo);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(ListCommentsForVideoRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videoId = Route<Guid>("videoId");
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        try
        {
            var anonymousId = CommentMapper.ResolveAnonymousId(HttpContext.Request);

            var (comments, totalCount) = await commentService.GetCommentsForVideoAsync(
                userId, anonymousId, videoId, pageNumber, pageSize, ct);

            var shaOfAnonymousId = CommentMapper.ComputeSha256(anonymousId);

            var response = new PagedResponse<CommentResponse>
            {
                Items = comments
                    .Select(c => CommentMapper.MapToResponse(c, userId, shaOfAnonymousId))
                    .ToArray(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            await Send.OkAsync(response, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Failed to get comments for video {VideoId}. User unauthorized", videoId);
            await Send.UnauthorizedAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to get comments for video {VideoId}", videoId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
