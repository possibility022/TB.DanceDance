using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class PublishThumbnailEndpoint : EndpointWithoutRequest
{
    private readonly IVideoUploaderService videoUploaderService;

    public PublishThumbnailEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Converter.PublishThumbnail);
        Policies(ApiScopes.Convert);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var videoId = Route<Guid>("videoId");
        var published = await videoUploaderService.PublishThumbnailAsync(videoId, ct);
        if (!published)
            return await Send.ErrorsAsync(cancellation: ct);

        return await Send.NoContentAsync(ct);
    }
}
