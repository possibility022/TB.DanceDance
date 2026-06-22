using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Deletes an owned competition, detaching (not deleting) its videos. Requires authentication.
/// </summary>
public class DeleteCompetitionEndpoint : EndpointWithoutRequest
{
    private readonly ICompetitionService competitionService;

    public DeleteCompetitionEndpoint(ICompetitionService competitionService)
    {
        this.competitionService = competitionService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Competitions.Delete);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");

        var deleted = await competitionService.DeleteAsync(competitionId, userId, ct);
        if (!deleted)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
