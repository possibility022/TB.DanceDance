using FastEndpoints;
using SharedVideoInfoResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedVideoInfoResponse;

namespace Application.Features.Sharing.Endpoints;

public record VideoInfoBySharedLinkRequest
{
    /// <summary>The shared link id (bound from the route).</summary>
    public string LinkId { get; set; } = null!;
}

/// <summary>
/// Gets video information by shared link id. Anonymous access allowed.
/// </summary>
public class GetVideoInfoBySharedLinkEndpoint : Endpoint<VideoInfoBySharedLinkRequest, SharedVideoInfoResponse>
{
    private readonly ISharedLinkService sharedLinkService;

    public GetVideoInfoBySharedLinkEndpoint(ISharedLinkService sharedLinkService)
    {
        this.sharedLinkService = sharedLinkService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Share.GetInfo);
        AllowAnonymous();
    }

    public override async Task HandleAsync(VideoInfoBySharedLinkRequest req, CancellationToken ct)
    {
        var link = await sharedLinkService.GetSharedLinkAsync(req.LinkId, ct);

        if (link == null || link.Video == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = ShareMapper.MapToSharedVideoInfoResponse(link);
        await Send.OkAsync(response, ct);
    }
}
