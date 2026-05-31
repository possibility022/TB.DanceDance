using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.Access.Events;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using ApiEvent = TB.DanceDance.API.Contracts.Models.Event;

namespace TB.DanceDance.API.Features.Events;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class EventsController : Controller
{
    private readonly IRequestHandler<CreateEventCommand, Guid> createEventCommand;
    private readonly IRequestHandler<ViewVideosFromEventQuery, IReadOnlyCollection<VideoDto>> viewVideosFromEventQuery;
    private readonly IRequestHandler<DoesUserHasAccessToSharedWith, bool> doesUserHasAccessToSharedWithQuery;

    public EventsController(IRequestHandler<CreateEventCommand, Guid> createEventCommand,
        IRequestHandler<ViewVideosFromEventQuery, IReadOnlyCollection<VideoDto>> viewVideosFromEventQuery,
        IRequestHandler<DoesUserHasAccessToSharedWith, bool> doesUserHasAccessToSharedWithQuery)
    {
        this.createEventCommand = createEventCommand;
        this.viewVideosFromEventQuery = viewVideosFromEventQuery;
        this.doesUserHasAccessToSharedWithQuery = doesUserHasAccessToSharedWithQuery;
    }

    [HttpPost]
    [Route(EventRoutes.AddEvent)]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateNewEventRequest request, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ownerId = User.GetSubject();
        var date = request.Event.Date.ToUniversalTime();

        var eventId = await createEventCommand.HandleAsync(
            new CreateEventCommand(request.Event.Name, date, ownerId), token);

        var created = new ApiEvent()
        {
            Id = eventId,
            Name = request.Event.Name,
            Date = date,
        };

        return Created("", created);
    }

    [HttpGet]
    [Route(EventRoutes.Videos)]
    public async Task<IActionResult> GetEventVideos([FromRoute] Guid eventId, CancellationToken token)
    {
        var userId = User.GetSubject();
        var videos = await viewVideosFromEventQuery.HandleAsync(new ViewVideosFromEventQuery(userId, eventId), token);

        if (videos.Count == 0)
        {
            var isAssigned = await doesUserHasAccessToSharedWithQuery.HandleAsync(new DoesUserHasAccessToSharedWith
            {
                UserId = userId,
                SharedToId = eventId,
                SharedWithType = SharedWithByType.Event,
            }, token);

            if (!isAssigned)
                return Unauthorized();
        }

        var results = videos
            .Select(ContractMappers.MapToVideoInformation)
            .ToList();

        return Ok(results);
    }
}
