using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Deletes a comment. Can be deleted by the author or the video owner. Anonymous access allowed.
/// </summary>
public class DeleteCommentEndpoint : Endpoint<DeleteCommentRequest>
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

    public override async Task HandleAsync(DeleteCommentRequest req, CancellationToken ct)
    {
        var userId = User.TryGetSubject();
        var anonymousId = CommentMapper.ResolveAnonymousId(req.AnonymousId, HttpContext.Request);

        try
        {
            var result = await commentService.DeleteCommentAsync(
                req.CommentId,
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

        await Send.OkAsync(ct);
    }
}