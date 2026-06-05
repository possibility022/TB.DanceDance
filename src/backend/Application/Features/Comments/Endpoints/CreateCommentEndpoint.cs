using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;
using CommentResponse = TB.DanceDance.API.Contracts.Features.Comments.CommentResponse;

namespace Application.Features.Comments.Endpoints;

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
        var linkId = Route<string>("linkId") ?? string.Empty;

        try
        {
            var comment = await commentService.CreateCommentAsync(
                userId,
                linkId,
                req.Content,
                req.AuthorName,
                req.AnonymousId,
                ct);

            var response = CommentMapper.MapToResponse(comment, userId, null);
            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to create comment through link {LinkId}", linkId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}