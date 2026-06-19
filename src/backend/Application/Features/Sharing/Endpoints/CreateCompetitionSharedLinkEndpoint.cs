using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Sharing;
using SharedLinkResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedLinkResponse;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Creates a shared link that targets a whole competition. Requires authentication; owner only.
    /// </summary>
    public class CreateCompetitionSharedLinkEndpoint : Endpoint<CreateSharedLinkRequest, SharedLinkResponse>
    {
        private readonly ISharedLinkService sharedLinkService;
        private readonly IOptions<AppOptions> appOptions;

        public CreateCompetitionSharedLinkEndpoint(ISharedLinkService sharedLinkService, IOptions<AppOptions> appOptions)
        {
            this.sharedLinkService = sharedLinkService;
            this.appOptions = appOptions;
        }

        public override void Configure()
        {
            Post(ApiRoutes.Share.CreateForCompetition);
            Policies(ApiScopes.Read);
        }

        public override async Task HandleAsync(CreateSharedLinkRequest req, CancellationToken ct)
        {
            var userId = User.GetSubject();
            var competitionId = Route<Guid>("competitionId");

            try
            {
                var link = await sharedLinkService.CreateCompetitionSharedLinkAsync(
                    competitionId,
                    userId,
                    req.ExpirationDays,
                    req.AllowComments,
                    req.AllowAnonymousComments,
                    ct);

                var response = ShareMapper.MapToSharedLinkResponse(link, appOptions.Value.AppWebsiteOrigin);
                await Send.OkAsync(response, ct);
            }
            catch (ArgumentException ex)
            {
                Logger.LogWarning(ex, "Failed to create competition shared link for {CompetitionId} by user {UserId}", competitionId, userId);
                AddError(ex.Message);
                await Send.ErrorsAsync(400, ct);
            }
        }
    }
}
