using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TB.DanceDance.API.Contracts.Features.Competitions;

namespace Application.Features.Competitions.Endpoints;

/// <summary>
/// Renames an owned competition. Requires authentication.
/// </summary>
public class RenameCompetitionEndpoint : Endpoint<RenameCompetitionRequest>
{
    private readonly ICompetitionService competitionService;

    public RenameCompetitionEndpoint(ICompetitionService competitionService)
    {
        this.competitionService = competitionService;
    }

    public override void Configure()
    {
        Patch(ApiRoutes.Competitions.Rename);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(RenameCompetitionRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var competitionId = Route<Guid>("competitionId");

        try
        {
            var renamed = await competitionService.RenameAsync(competitionId, userId, req.NewName, ct);
            if (!renamed)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to rename competition {CompetitionId}", competitionId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
