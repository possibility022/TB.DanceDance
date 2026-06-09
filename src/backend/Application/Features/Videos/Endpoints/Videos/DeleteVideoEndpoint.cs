using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Videos.Endpoints.Videos;

public class DeleteVideoEndpoint : EndpointWithoutRequest
{
    private readonly IVideoService videoService;

    public DeleteVideoEndpoint(IVideoService videoService)
    {
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Video.Delete);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videoId = Route<Guid>("videoId");

        var result = await videoService.DeleteVideoAsync(videoId, userId, ct);

        switch (result)
        {
            case DeleteVideoResult.NotFound:
                await Send.NotFoundAsync(ct);
                break;
            case DeleteVideoResult.Forbidden:
                await Send.ForbiddenAsync(ct);
                break;
            default:
                await Send.NoContentAsync(ct);
                break;
        }
    }
}
