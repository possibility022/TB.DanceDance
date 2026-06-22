using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Comments;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Comments.Endpoints;

/// <summary>
/// Gets the combined comment thread for a competition the authenticated user owns, directly by id
/// (not via a shared link). Owner-only.
/// </summary>
public class ListCommentsForCompetitionEndpoint : Endpoint<ListCommentsForCompetitionRequest, PagedResponse<CommentResponse>>
{
    private readonly ICommentService commentService;

    public ListCommentsForCompetitionEndpoint(ICommentService commentService)
    {
        this.commentService = commentService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Comments.ListCommentsForCompetition);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(ListCommentsForCompetitionRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        try
        {
            var (comments, totalCount) = await commentService.GetCommentsForCompetitionAsync(
                userId, competitionId, pageNumber, pageSize, ct);

            var response = new PagedResponse<CommentResponse>
            {
                Items = comments
                    .Select(c => CommentMapper.MapToResponse(c, userId, anonymousId: null))
                    .ToArray(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            await Send.OkAsync(response, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Failed to get comments for competition {CompetitionId}. User unauthorized", competitionId);
            await Send.UnauthorizedAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to get comments for competition {CompetitionId}", competitionId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
