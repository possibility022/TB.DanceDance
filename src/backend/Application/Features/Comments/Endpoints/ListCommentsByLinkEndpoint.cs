using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Gets comments for a video accessed through a shared link. Anonymous access allowed.
/// </summary>
public class ListCommentsByLinkEndpoint : EndpointWithoutRequest<ListCommentsByLinkResponse>
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.TryGetSubject();
        var linkId = Route<string>("linkId") ?? string.Empty;
        
        var anonymousId = CommentMapper.ResolveAnonymousId(HttpContext.Request);

        try
        {
            var comments = await commentService.GetCommentsForVideoAsync(
                userId,
                anonymousId,
                linkId,
                ct);

            var shaOfAnonymousId = CommentMapper.ComputeSha256(anonymousId);

            var response = new ListCommentsByLinkResponse
            {
                Comments = comments
                    .Select(c => CommentMapper.MapToResponse(c, userId, shaOfAnonymousId))
                    .ToArray(),
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