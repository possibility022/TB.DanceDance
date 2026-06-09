using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Videos.Endpoints.Videos;

public class ListMyVideosEndpoint : Endpoint<ListMyVideosRequest, PagedResponse<VideoInformation>>
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

    public override async Task HandleAsync(ListMyVideosRequest req, CancellationToken ct)
    {
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        var userId = User.GetSubject();
        var query = accessService.GetUserPrivateVideosQuery(userId);
        var (videos, totalCount) = await query.ToPagedResultAsync(pageNumber, pageSize, ct);

        var videoInformation = videos
            .Select(v => ContractMappers.MapToVideoInformation(v, thumbnailUrlService.GetThumbnailUrl(v.ThumbnailBlobId), userId))
            .ToArray();

        await Send.OkAsync(new PagedResponse<VideoInformation>
        {
            Items = videoInformation,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellation: ct);
    }
}
