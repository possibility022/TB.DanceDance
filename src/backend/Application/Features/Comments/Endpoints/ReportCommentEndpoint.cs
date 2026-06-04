using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Application.Features.Comments.Endpoints;

public record ReportCommentRequest
{
    /// <summary>The comment id (bound from the route).</summary>
    public Guid CommentId { get; set; }

    /// <summary>The reason for reporting this comment.</summary>
    public string Reason { get; set; } = null!;
}

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
        try
        {
            var result = await commentService.ReportCommentAsync(req.CommentId, req.Reason, ct);

            if (!result)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to report comment {CommentId}", req.CommentId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
