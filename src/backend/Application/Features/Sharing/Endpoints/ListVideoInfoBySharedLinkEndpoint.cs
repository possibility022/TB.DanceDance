using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Sharing;

namespace Application.Features.Sharing.Endpoints
{
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
}