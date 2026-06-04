using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Unhides a comment. Only the video owner can unhide comments.
/// </summary>
public class UnhideCommentEndpoint : Endpoint<UnhideCommentRequest>
{
    private readonly ICommentService commentService;

    public UnhideCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Comments.Unhide);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(UnhideCommentRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        var result = await commentService.UnhideCommentAsync(req.CommentId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}