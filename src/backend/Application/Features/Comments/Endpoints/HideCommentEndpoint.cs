using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Comments.Endpoints;

public record HideCommentRequest
{
    /// <summary>The comment id (bound from the route).</summary>
    public Guid CommentId { get; set; }
}

/// <summary>
/// Hides a comment. Only the video owner can hide comments.
/// </summary>
public class HideCommentEndpoint : Endpoint<HideCommentRequest>
{
    private readonly ICommentService commentService;

    public HideCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Comments.Hide);
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
    }

    public override async Task HandleAsync(HideCommentRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        var result = await commentService.HideCommentAsync(req.CommentId, userId, ct);

        if (!result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
