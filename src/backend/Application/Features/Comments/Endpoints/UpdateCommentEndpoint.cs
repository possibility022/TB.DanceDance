using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Updates a comment. Only the authenticated comment author (or the matching anonymous author) can update.
/// </summary>
public class UpdateCommentEndpoint : Endpoint<UpdateCommentRequest>
{
    private readonly ICommentService commentService;

    public UpdateCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Comments.Update);
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateCommentRequest req, CancellationToken ct)
    {
        var userId = User.TryGetSubject();

        // Authenticated users edit by identity, never by anonymous id.
        if (userId is not null)
            req.AnonymousId = null;

        try
        {
            var result = await commentService.UpdateCommentAsync(
                req.CommentId,
                userId,
                req.AnonymousId,
                req.AuthorName,
                req.Content,
                ct);

            if (!result)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to update comment {CommentId}", req.CommentId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}