using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Competitions;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Lists the current user's competitions, newest first. Requires authentication.
/// </summary>
public class ListMyCompetitionsEndpoint : EndpointWithoutRequest<ListMyCompetitionsResponse>
{
    private readonly ICompetitionService competitionService;

    public ListMyCompetitionsEndpoint(ICompetitionService competitionService)
    {
        this.competitionService = competitionService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Competitions.ListMy);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitions = await competitionService.ListMyCompetitionsAsync(userId, ct);

        await Send.OkAsync(new ListMyCompetitionsResponse
        {
            Competitions = competitions.Select(CompetitionMapper.MapToSummary).ToArray()
        }, ct);
    }
}
