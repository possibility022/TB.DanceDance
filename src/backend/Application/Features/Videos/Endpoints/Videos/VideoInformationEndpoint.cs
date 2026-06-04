using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos.Endpoints.Videos;

public class VideoInformationEndpoint : Endpoint<VideoInformationRequest, VideoInformationResponse>
{
    private readonly IVideoService videoService;

    public VideoInformationEndpoint(IVideoService videoService)
    {
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.GetSingle);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(VideoInformationRequest req, CancellationToken ct)
    {
        string user = User.GetSubject();

        var info = await videoService.GetVideoByBlobAsync(user, req.BlobId, ct);

        if (info == null)
            await Send.NotFoundAsync(cancellation: ct);
        else
        {
            var results = ContractMappers.MapToVideoInformation(info);
            await Send.OkAsync(new VideoInformationResponse
            {
                VideoInformation = results
            }, ct);
        }   
    }
}