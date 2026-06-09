using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos.Endpoints.Videos;

public class VideoInformationEndpoint : EndpointWithoutRequest<VideoInformationResponse>
{
    private readonly IVideoService videoService;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public VideoInformationEndpoint(IVideoService videoService, IThumbnailUrlService thumbnailUrlService)
    {
        this.videoService = videoService;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.GetSingle);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        string user = User.GetSubject();
        
        var blobId = Route<string>("blobId") ?? string.Empty;

        var info = await videoService.GetVideoByBlobAsync(user, blobId, ct);

        if (info == null)
            await Send.NotFoundAsync(cancellation: ct);
        else
        {
            var results = ContractMappers.MapToVideoInformation(info, thumbnailUrlService.GetThumbnailUrl(info.ThumbnailBlobId), user);
            await Send.OkAsync(new VideoInformationResponse
            {
                VideoInformation = results
            }, ct);
        }   
    }
}