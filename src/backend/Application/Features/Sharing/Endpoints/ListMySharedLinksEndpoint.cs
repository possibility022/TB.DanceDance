using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Options;
using SharedLinkResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedLinkResponse;

namespace Application.Features.Sharing.Endpoints;

public record ListMySharedLinksResponse
{
    public required IReadOnlyCollection<SharedLinkResponse> Links { get; init; }
}

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
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
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
