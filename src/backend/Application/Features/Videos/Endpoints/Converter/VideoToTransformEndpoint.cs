using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using TB.DanceDance.API.Contracts.Features.Videos.Models;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class VideoToTransformEndpoint : EndpointWithoutRequest<VideoToTransformResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public VideoToTransformEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Converter.Videos);
        Policies(ApiScopes.Convert);
    }
    
    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync(ct);

        if (video == null)
            return await Send.OkAsync(new VideoToTransformResponse() { VideoExists = false, VideoToTransform = null }, ct);

        var sas = videoUploaderService.GetVideoSas(video.SourceBlobId);

        return await Send.OkAsync(new VideoToTransformResponse()
        {
            VideoExists = true,
            VideoToTransform = new VideoToTransformModel()
            {
                Id = video.Id,
                FileName = video.FileName,
                Sas = sas.ToString(),
            }
        }, ct);
    }
}