using Application.Extensions;
using Application.Features.Videos;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Competitions;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Gets a single owned competition with its videos. Requires authentication.
/// </summary>
public class GetCompetitionEndpoint : EndpointWithoutRequest<CompetitionResponse>
{
    private readonly ICompetitionService competitionService;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public GetCompetitionEndpoint(ICompetitionService competitionService, IThumbnailUrlService thumbnailUrlService)
    {
        this.competitionService = competitionService;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Competitions.Get);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");

        var competition = await competitionService.GetAsync(competitionId, userId, ct);
        if (competition == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(CompetitionMapper.MapToResponse(competition, thumbnailUrlService, userId), ct);
    }
}
