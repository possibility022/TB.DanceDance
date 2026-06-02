using Application.Features.AccessManagement;
using Application.Features.Events;
using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Features.Events;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;

namespace TB.DanceDance.API.Features.Events;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class EventsController : Controller
{
    private readonly IAccessService accessService;
    private readonly IEventService eventService;

    public EventsController(IAccessService accessService, IEventService eventService)
    {
        this.accessService = accessService;
        this.eventService = eventService;
    }

    [HttpPost]
    [Route(EventRoutes.AddEvent)]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateNewEventRequest request, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var @event = ContractMappers.MapFromNewEventRequestToEvent(request, User);

        if (!ModelState.IsValid)
            return BadRequest();

        var createdEvent = await eventService.CreateEventAsync(@event, token);


        return Created("", createdEvent); //todo
    }

    [HttpGet]
    [Route(EventRoutes.Videos)]
    public async Task<IActionResult> GetEventVideos([FromRoute] Guid eventId, CancellationToken token)
    {
        var userId = User.GetSubject();
        var videos = await eventService
            .GetVideos(eventId, userId, token);

        if (videos.Length == 0)
        {
            var isAssigned = await accessService.DoesUserHasAccessToEvent(eventId, userId, token);
            if (!isAssigned)
                return Unauthorized();
        }

        var results = videos
            .Select(r => ContractMappers.MapToVideoInformation(r))
            .ToList();

        return Ok(results);
    }
}
