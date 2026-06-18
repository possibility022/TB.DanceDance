using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Removes a video from an owned competition (leaves it standalone). Requires authentication.
/// </summary>
public class RemoveVideoFromCompetitionEndpoint : EndpointWithoutRequest
{
    private readonly ICompetitionService competitionService;

    public RemoveVideoFromCompetitionEndpoint(ICompetitionService competitionService)
    {
        this.competitionService = competitionService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Competitions.RemoveVideo);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");
        var videoId = Route<Guid>("videoId");

        var removed = await competitionService.RemoveVideoAsync(competitionId, videoId, userId, ct);
        if (!removed)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
