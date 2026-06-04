using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;


public record VideoToTransformResponse
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }

    public required string Sas { get; init; }
}

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
    }
    
    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync(ct);

        if (video == null)
            return await Send.NotFoundAsync(ct);

        var sas = videoUploaderService.GetVideoSas(video.SourceBlobId);

        return await Send.OkAsync(new VideoToTransformResponse()
        {
            Id = video.Id,
            FileName = video.FileName,
            Sas = sas.ToString(),
        }, ct);
    }
}