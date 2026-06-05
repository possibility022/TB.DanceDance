using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Unhides a comment. Only the video owner can unhide comments.
/// </summary>
public class UnhideCommentEndpoint : EndpointWithoutRequest
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

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var commentId = Route<Guid>("commentId");

        var result = await commentService.UnhideCommentAsync(commentId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}