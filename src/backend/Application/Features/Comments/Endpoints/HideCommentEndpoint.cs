using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Hides a comment. Only the video owner can hide comments.
/// </summary>
public class HideCommentEndpoint : EndpointWithoutRequest
{
    private readonly ICommentService commentService;

    public HideCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Comments.Hide);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var commentId = Route<Guid>("commentId");

        var result = await commentService.HideCommentAsync(commentId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}