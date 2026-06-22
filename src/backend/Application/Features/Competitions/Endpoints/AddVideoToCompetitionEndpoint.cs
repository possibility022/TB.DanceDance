using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Adds an owned video to an owned competition. Requires authentication.
/// </summary>
public class AddVideoToCompetitionEndpoint : EndpointWithoutRequest
{
    private readonly ICompetitionService competitionService;

    public AddVideoToCompetitionEndpoint(ICompetitionService competitionService)
    {
        this.competitionService = competitionService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Competitions.AddVideo);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");
        var videoId = Route<Guid>("videoId");

        try
        {
            await competitionService.AddVideoAsync(competitionId, videoId, userId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to add video {VideoId} to competition {CompetitionId}", videoId, competitionId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
