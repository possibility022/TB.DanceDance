using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos.Endpoints.Videos;

public class ListMyVideosEndpoint : EndpointWithoutRequest<MyVideosResponse>
{
    private readonly IAccessService accessService;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public ListMyVideosEndpoint(IAccessService accessService, IThumbnailUrlService thumbnailUrlService)
    {
        this.accessService = accessService;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.MyVideos);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var videos = await accessService.GetUserPrivateVideos(User.GetSubject(), ct);
        var videoInformation = videos
            .Select(v => ContractMappers.MapToVideoInformation(v, thumbnailUrlService.GetThumbnailUrl(v.ThumbnailBlobId)))
            .ToArray();
        
        await Send.OkAsync(new MyVideosResponse
        {
            VideoInformation = videoInformation
        }, cancellation: ct);
    }
}