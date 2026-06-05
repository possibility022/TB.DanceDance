using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Deletes a comment. Can be deleted by the author or the video owner. Anonymous access allowed.
/// </summary>
public class DeleteCommentEndpoint : EndpointWithoutRequest
{
    private readonly ICommentService commentService;

    public DeleteCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Comments.Delete);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.TryGetSubject();
        var commendId = Route<Guid>("commendId");
        
        var anonymousId = CommentMapper.ResolveAnonymousId(HttpContext.Request);

        try
        {
            var result = await commentService.DeleteCommentAsync(
                commendId,
                userId,
                anonymousId,
                ct);

            if (!result)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
        }
        catch (ArgumentException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}