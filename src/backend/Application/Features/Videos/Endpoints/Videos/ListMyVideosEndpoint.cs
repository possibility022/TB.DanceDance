using Application.Extensions;
using Application.Features.AccessManagement;
using Application.Features.Groups.Models;
using FastEndpoints;

namespace Application.Features.Videos.Endpoints.Videos;

public record MyVideosResponse
{
    public required ICollection<VideoInformation> VideoInformation { get; set; }
}

public class ListMyVideosEndpoint : EndpointWithoutRequest<MyVideosResponse>
{
    private readonly IAccessService accessService;

    public ListMyVideosEndpoint(IAccessService accessService)
    {
        this.accessService = accessService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.MyVideos);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var videos = await accessService.GetUserPrivateVideos(User.GetSubject(), ct);
        var videoInformation = videos.Select(ContractMappers.MapToVideoInformation).ToArray();
        
        await Send.OkAsync(new MyVideosResponse
        {
            VideoInformation = videoInformation
        }, cancellation: ct);
    }
}