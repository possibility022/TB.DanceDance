using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using CommentResponse = TB.DanceDance.API.Contracts.Features.Comments.CommentResponse;

namespace Application.Features.Comments.Endpoints;

public record CreateCommentRequest
{
    /// <summary>Shared link id (bound from the route) the comment is posted through.</summary>
    public string LinkId { get; set; } = null!;

    /// <summary>The comment content.</summary>
    public string Content { get; set; } = null!;

    /// <summary>Client-side anonymous id, allowing anonymous authors to edit their comment later.</summary>
    public string? AnonymousId { get; set; }

    /// <summary>Display name used when posting as anonymous.</summary>
    public string? AuthorName { get; set; }
}

/// <summary>
/// Creates a comment on a video through a shared link. Anonymous access allowed.
/// </summary>
public class CreateCommentEndpoint : Endpoint<CreateCommentRequest, CommentResponse>
{
    private readonly ICommentService commentService;

    public CreateCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Comments.Create);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateCommentRequest req, CancellationToken ct)
    {
        // userId is present for authenticated users, null for anonymous.
        var userId = User.TryGetSubject();

        try
        {
            var comment = await commentService.CreateCommentAsync(
                userId,
                req.LinkId,
                req.Content,
                req.AuthorName,
                req.AnonymousId,
                ct);

            var response = CommentMapper.MapToResponse(comment, userId, null);
            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to create comment through link {LinkId}", req.LinkId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
