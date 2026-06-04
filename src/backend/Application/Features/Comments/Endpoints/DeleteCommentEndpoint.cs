using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Comments.Endpoints;

public record DeleteCommentRequest
{
    /// <summary>The comment id (bound from the route).</summary>
    public Guid CommentId { get; set; }

    /// <summary>Anonymous id (bound from the query string); falls back to the request header.</summary>
    public string? AnonymousId { get; set; }
}

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
