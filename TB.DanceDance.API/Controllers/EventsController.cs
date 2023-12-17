using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;
using static IdentityModel.OidcConstants;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class EventsController : Controller
{
    private readonly IUserService userService;
    private readonly IEventService eventService;

    public EventsController(IUserService userService, IEventService eventService)
    {
        this.userService = userService;
        this.eventService = eventService;
    }

    [Route(ApiEndpoints.Video.Access.GetAll)]
    [HttpGet]
    public async Task<EventsAndGroupsResponse> GetAllEventsAndGroups()
    {
        var listOfEvents = await userService.GetAllEvents();
        var listOfGroups = await userService.GetAllGroups();

        return new EventsAndGroupsResponse()
        {
            Events = listOfEvents
                .Select(@event => ContractMappers.MapToEventContract(@event))
                .ToList(),
            Groups = listOfGroups
                .Select(group => ContractMappers.MapToGroupContract(group))
                .ToList()
        };
    }

    [Route(ApiEndpoints.Video.Access.GetUserAccess)]
    public async Task<UserEventsAndGroupsResponse> GetAssignedGroupsAsync()
    {
        var user = User.GetSubject();
        (var userGroups, var userEvents) = userService.GetUserEventsAndGroups(user);

        var responseModel = new UserEventsAndGroupsResponse();

        responseModel.Assigned.Groups = userGroups
            .Select(group => ContractMappers.MapToGroupContract(group))
                .ToArray();

        responseModel.Assigned.Events = userEvents
                .OrderByDescending(r => r.Date)
                .Select(@event => ContractMappers.MapToEventContract(@event))
                .ToArray();

        var listOfEvents = await userService.GetAllEvents();

        responseModel.Available.Events = listOfEvents.Except(userEvents)
            .Select(@event => ContractMappers.MapToEventContract(@event))
            .ToArray();

        var listOfGroups = await userService.GetAllGroups();

        responseModel.Available.Groups = listOfGroups.Except(userGroups)
            .Select(group => ContractMappers.MapToGroupContract(group))
            .ToArray();

        return responseModel;
    }

    [HttpPost]
    [Route(ApiEndpoints.Event.AddEvent)]
    public async Task<IActionResult> CreateEventAsync([FromBody] CreateNewEventRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);


        var @event = ContractMappers.MapFromNewEventRequestToEvent(request);
        var user = User.GetSubject();


        if (!ModelState.IsValid)
            return BadRequest();

        var createdEvent = await eventService.CreateEventAsync(@event, user);


        return Created("", createdEvent); //todo
    }

    [Route(ApiEndpoints.Video.Access.RequestAccess)]
    [HttpPost]
    public async Task<IActionResult> RequestAssigment([FromBody] RequestEventAssigmentModelRequest requests)
    {
        if (requests == null)
            return BadRequest();

        if (requests.Events?.Any() != true && requests.Groups?.Any() != true)
            return BadRequest();

        if (requests.Groups != null)
        {
            if (requests.Groups.Any(r => r.JoinedDate == default))
                return BadRequest();
        }

        var user = User.GetSubject();

        var tasks = new Task[] { Task.CompletedTask, Task.CompletedTask };

        if (requests.Events?.Count > 0)
            tasks[0] = userService.SaveEventsAssigmentRequest(user, requests.Events);

        if (requests.Groups?.Count > 0)
        {
            var model = requests.Groups.Select(r => (r.Id, r.JoinedDate)).ToArray();
            tasks[1] = userService.SaveGroupsAssigmentRequests(user, model);
        }

        await Task.WhenAll(tasks);

        return Ok();
    }

    [HttpGet]
    [Route(ApiEndpoints.Event.Videos)]
    public async Task<IActionResult> GetEventVideos([FromRoute] Guid eventId)
    {
        var userId = User.GetSubject();
        var videos = await eventService
            .GetVideos(eventId, userId)
            .ToListAsync();

        if (videos.Count == 0)
        {
            var isAssigned = eventService.IsUserAssignedToEvent(eventId, userId);
            if (!isAssigned)
                return Unauthorized();
        }

        var results = videos
            .Select(r => ContractMappers.MapToVideoInformation(r))
            .ToList();

        return Ok(results);
    }


}
