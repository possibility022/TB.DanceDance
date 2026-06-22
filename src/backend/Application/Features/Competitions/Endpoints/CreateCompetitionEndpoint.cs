using Application.Extensions;
using Application.Features.Videos;
using Domain.Entities;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Competitions;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Creates a new competition owned by the current user. Requires authentication.
/// </summary>
public class CreateCompetitionEndpoint : Endpoint<CreateCompetitionRequest, CompetitionResponse>
{
    private readonly ICompetitionService competitionService;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public CreateCompetitionEndpoint(ICompetitionService competitionService, IThumbnailUrlService thumbnailUrlService)
    {
        this.competitionService = competitionService;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Competitions.Create);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CreateCompetitionRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        try
        {
            var competition = await competitionService.CreateAsync(
                userId, req.Name, req.Date, req.Location, (CommentVisibility)req.CommentVisibility, ct);

            competition.Videos ??= [];
            await Send.OkAsync(CompetitionMapper.MapToResponse(competition, thumbnailUrlService, userId), ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to create competition for user {UserId}", userId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
