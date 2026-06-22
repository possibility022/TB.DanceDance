using Application.Features.Videos;
using FastEndpoints;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Streams one specific video reachable through a shared link (the link's single video, or one of
    /// its competition's videos). Anonymous access allowed.
    /// </summary>
    public class StreamCompetitionVideoBySharedLinkEndpoint : EndpointWithoutRequest
    {
        private readonly ISharedLinkService sharedLinkService;
        private readonly IVideoService videoService;

        public StreamCompetitionVideoBySharedLinkEndpoint(ISharedLinkService sharedLinkService, IVideoService videoService)
        {
            this.sharedLinkService = sharedLinkService;
            this.videoService = videoService;
        }

        public override void Configure()
        {
            Get(ApiRoutes.Share.GetVideoStream);
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            var linkId = Route<string>("linkId") ?? string.Empty;
            var videoId = Route<Guid>("videoId");

            var video = await sharedLinkService.GetVideoForSharedLinkAsync(linkId, videoId, ct);

            if (video == null || string.IsNullOrEmpty(video.BlobId))
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            var stream = await videoService.OpenStream(video.BlobId, ct);
            await Send.StreamAsync(stream, contentType: "video/mp4", enableRangeProcessing: true, cancellation: ct);
        }
    }
}
