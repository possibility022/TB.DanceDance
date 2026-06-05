using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Reports a comment as inappropriate. Anonymous access allowed.
/// </summary>
public class ReportCommentEndpoint : Endpoint<ReportCommentRequest>
{
    private readonly ICommentService commentService;

    public ReportCommentEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Comments.Report);
        AllowAnonymous();
    }

    public override async Task HandleAsync(ReportCommentRequest req, CancellationToken ct)
    {
        var commentId = Route<Guid>("commentId");
        
        try
        {
            var result = await commentService.ReportCommentAsync(commentId, req.Reason, ct);

            if (!result)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to report comment {CommentId}", commentId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}