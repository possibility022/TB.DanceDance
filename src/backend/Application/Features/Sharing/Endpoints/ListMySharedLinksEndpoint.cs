using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Gets all shared links created by the user or for videos owned by the user. Requires authentication.
    /// </summary>
    public class ListMySharedLinksEndpoint : EndpointWithoutRequest<ListMySharedLinksResponse>
    {
        private readonly ISharedLinkService sharedLinkService;
        private readonly IOptions<AppOptions> appOptions;

        public ListMySharedLinksEndpoint(ISharedLinkService sharedLinkService, IOptions<AppOptions> appOptions)
        {
            this.sharedLinkService = sharedLinkService;
            this.appOptions = appOptions;
        }

        public override void Configure()
        {
            Get(ApiRoutes.Share.ListMy);
            Policies(ApiScopes.Read);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var userId = User.GetSubject();

            var links = await sharedLinkService.GetUserSharedLinksAsync(userId, ct);
            var origin = appOptions.Value.AppWebsiteOrigin;

            var response = new ListMySharedLinksResponse
            {
                Links = links
                    .Select(link => ShareMapper.MapToSharedLinkResponse(link, origin))
                    .ToArray(),
            };

            await Send.OkAsync(response, ct);
        }
    }
}
