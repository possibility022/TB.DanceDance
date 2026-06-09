using Application.Extensions;
using Application.Features.AccessManagement;
using Application.Features.Videos;
using Domain.Entities;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.Events.Endpoints;

public class ListEventVideosEndpoint : Endpoint<ListEventVideosRequest, PagedResponse<VideoInformation>>
{
    private readonly IEventService eventService;
    private readonly IAccessService accessService;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public ListEventVideosEndpoint(IEventService eventService, IAccessService accessService, IThumbnailUrlService thumbnailUrlService)
    {
        this.eventService = eventService;
        this.accessService = accessService;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Events.Videos);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(ListEventVideosRequest req, CancellationToken ct)
    {
        var eventId = Route<Guid>("eventId");
        var userId = User.GetSubject();
        var pageNumber = req.NormalizedPage;
        var pageSize = req.NormalizedPageSize;

        var (videos, totalCount) = await eventService.GetVideos(eventId, userId, pageNumber, pageSize, ct);

        if (totalCount == 0)
        {
            var isAssigned = await accessService.DoesUserHasAccessToEvent(eventId, userId, ct);
            if (!isAssigned)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
        }

        var response = new PagedResponse<VideoInformation>
        {
            Items = MapVideos(videos, userId),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        await Send.OkAsync(response, ct);
    }

    private VideoInformation[] MapVideos(IReadOnlyCollection<Video> videos, string currentUserId)
    {
        return videos
            .Select(v => ContractMappers.MapToVideoInformation(v, thumbnailUrlService.GetThumbnailUrl(v.ThumbnailBlobId), currentUserId))
            .ToArray();
    }
}
