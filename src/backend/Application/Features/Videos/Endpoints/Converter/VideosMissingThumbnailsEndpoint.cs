using FastEndpoints;
using FluentValidation;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;

namespace Application.Features.Videos.Endpoints.Converter;

public class VideosMissingThumbnailsValidator : Validator<VideosMissingThumbnailsRequest>
{
    public VideosMissingThumbnailsValidator()
    {
        RuleFor(r => r.Limit).GreaterThan(0);
    }
}

public class VideosMissingThumbnailsEndpoint(IVideoUploaderService videoUploaderService)
    : Endpoint<VideosMissingThumbnailsRequest, List<VideoMissingThumbnailModel>>
{
    public override void Configure()
    {
        Get(ApiRoutes.Converter.VideosMissingThumbnails);
        Policies(ApiScopes.Convert);
    }

    public override async Task HandleAsync(VideosMissingThumbnailsRequest req, CancellationToken ct)
    {
        var videos = await videoUploaderService.GetVideosMissingThumbnailsAsync(req.Limit, ct);
        await Send.OkAsync(videos, ct);
    }
}
