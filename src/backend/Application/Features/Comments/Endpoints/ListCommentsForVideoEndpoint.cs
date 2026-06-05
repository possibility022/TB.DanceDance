using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Gets comments for a video the authenticated user has access to.
/// </summary>
public class ListCommentsForVideoEndpoint : EndpointWithoutRequest<ListCommentsForVideoResponse>
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videoId = Route<Guid>("videoId");

        try
        {
            var anonymousId = CommentMapper.ResolveAnonymousId(HttpContext.Request);

            var comments = await commentService.GetCommentsForVideoAsync(userId, anonymousId, videoId, ct);

            var shaOfAnonymousId = CommentMapper.ComputeSha256(anonymousId);

            var response = new ListCommentsForVideoResponse
            {
                Comments = comments
                    .Select(c => CommentMapper.MapToResponse(c, userId, shaOfAnonymousId))
                    .ToArray(),
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