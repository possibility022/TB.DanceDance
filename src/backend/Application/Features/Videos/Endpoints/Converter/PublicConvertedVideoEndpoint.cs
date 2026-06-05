using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class PublicConvertedVideoEndpoint : EndpointWithoutRequest<PublicConvertedVideoResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public PublicConvertedVideoEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Converter.Upload);
        Policies(ApiScopes.Convert);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var videoId = Route<Guid>("videoId");
        var newId = await videoUploaderService.UploadConvertedVideoAsync(videoId, ct);
        if (newId == null)
            return await Send.ErrorsAsync(cancellation: ct);

        return await Send.OkAsync(new PublicConvertedVideoResponse() { VideoId = newId.Value }, ct);
    }
}