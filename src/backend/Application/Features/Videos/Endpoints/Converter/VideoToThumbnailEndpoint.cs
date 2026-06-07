using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class VideoToThumbnailEndpoint : EndpointWithoutRequest<VideoToThumbnailResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public VideoToThumbnailEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Converter.Thumbnails);
        Policies(ApiScopes.Convert);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var video = await videoUploaderService.GetNextVideoForThumbnailAsync(ct);

        if (video == null)
            return await Send.OkAsync(new VideoToThumbnailResponse { VideoExists = false }, ct);

        return await Send.OkAsync(new VideoToThumbnailResponse
        {
            VideoExists = true,
            VideoToThumbnail = new VideoToThumbnailModel
            {
                Id = video.Value.Id,
                BlobId = video.Value.BlobId,
                FileName = video.Value.FileName,
                Sas = video.Value.Sas.ToString()
            }
        }, ct);
    }
}
