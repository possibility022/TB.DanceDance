using Application.Features.Videos;
using FastEndpoints;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Streams a video by shared link id. Anonymous access allowed.
    /// </summary>
    public class StreamVideoBySharedLinkEndpoint : Endpoint<StreamVideoBySharedLinkRequest>
    {
        private readonly ISharedLinkService sharedLinkService;
        private readonly IVideoService videoService;

        public StreamVideoBySharedLinkEndpoint(ISharedLinkService sharedLinkService, IVideoService videoService)
        {
            this.sharedLinkService = sharedLinkService;
            this.videoService = videoService;
        }

        public override void Configure()
        {
            Get(ApiRoutes.Share.GetStream);
            AllowAnonymous();
        }

        public override async Task HandleAsync(StreamVideoBySharedLinkRequest req, CancellationToken ct)
        {
            var video = await sharedLinkService.GetVideoBySharedLinkAsync(req.LinkId, ct);

            if (video == null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            if (string.IsNullOrEmpty(video.BlobId))
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            var stream = await videoService.OpenStream(video.BlobId, ct);
            await Send.StreamAsync(stream, contentType: "video/mp4", enableRangeProcessing: true, cancellation: ct);
        }
    }
}
