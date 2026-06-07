using Application.Extensions;
using Application.Pagination;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Comments.Endpoints;

public class ListCommentsByLinkRequest : PagedRequest
{
}

/// <summary>
/// Gets comments for a video accessed through a shared link. Anonymous access allowed.
/// </summary>
public class ListCommentsByLinkEndpoint : Endpoint<ListCommentsByLinkRequest, PagedResponse<CommentResponse>>
{
    private readonly ICommentService commentService;

    public ListCommentsByLinkEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Comments.ListByLink);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListCommentsByLinkRequest req, CancellationToken ct)
    {
        var userId = User.TryGetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        var anonymousId = CommentMapper.ResolveAnonymousId(HttpContext.Request);

        try
        {
            var (comments, totalCount) = await commentService.GetCommentsForVideoAsync(
                userId,
                anonymousId,
                linkId,
                pageNumber,
                pageSize,
                ct);

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
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to get comments for link {LinkId}", linkId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
