using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class PublicConvertedVideoEndpoint : Endpoint<PublicConvertedVideoRequest, PublicConvertedVideoResponse>
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

    public override async Task<Void> HandleAsync(PublicConvertedVideoRequest req, CancellationToken ct)
    {
        var newId = await videoUploaderService.UploadConvertedVideoAsync(req.VideoId, ct);
        if (newId == null)
            return await Send.ErrorsAsync(cancellation: ct);

        return await Send.OkAsync(new PublicConvertedVideoResponse() { VideoId = newId.Value }, ct);
    }
}