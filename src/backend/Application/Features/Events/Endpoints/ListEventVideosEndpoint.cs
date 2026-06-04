using Application.Extensions;
using Application.Features.AccessManagement;
using Application.Features.Groups.Models;
using Domain.Entities;
using FastEndpoints;

namespace Application.Features.Events.Endpoints;

public record ListEventVideosRequest(Guid EventId);

public class ListEventVideosEndpoint : Endpoint<ListEventVideosRequest, ListEventVideosResponse>
{
    private readonly IEventService eventService;
    private readonly IAccessService accessService;

    public ListEventVideosEndpoint(IEventService eventService, IAccessService accessService)
    {
        this.eventService = eventService;
        this.accessService = accessService;
    }
    
    public override async Task HandleAsync(ListEventVideosRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();
        var videos = await eventService
            .GetVideos(req.EventId, userId, ct);

        if (videos.Length == 0)
        {
            var isAssigned = await accessService.DoesUserHasAccessToEvent(req.EventId, userId, ct);
            if (!isAssigned)
                await Send.UnauthorizedAsync(ct);
        }
        
        var videoInformation = MapVideos(videos);
        var response = new ListEventVideosResponse
        {
            Videos = videoInformation
        };
        
        await Send.OkAsync(response, ct);
    }

    private VideoInformation[] MapVideos(IReadOnlyCollection<Video> videos)
    {
        return videos.Select(ContractMappers.MapToVideoInformation).ToArray();
    }
}

public record ListEventVideosResponse
{
    public required IReadOnlyCollection<VideoInformation> Videos { get; init; }
}