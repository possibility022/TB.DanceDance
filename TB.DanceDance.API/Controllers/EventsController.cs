using Domain.Exceptions;
using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class EventsController : Controller
{
    private readonly IUserService userService;
    private readonly IEventService eventService;
    private readonly IIdentityClient identityClient;

    public EventsController(IUserService userService, IEventService eventService, IIdentityClient identityClient)
    {
        this.userService = userService;
        this.eventService = eventService;
        this.identityClient = identityClient;
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
        (var userGroups, var userEvents) = await userService.GetUserEventsAndGroupsAsync(user);

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


        var @event = ContractMappers.MapFromNewEventRequestToEvent(request, User);

        if (!ModelState.IsValid)
            return BadRequest();

        var createdEvent = await eventService.CreateEventAsync(@event);


        return Created("", createdEvent); //todo
    }

    [Route(ApiEndpoints.Video.Access.RequestAccess)]
    [HttpPost]
    public async Task<IActionResult> RequestAccess([FromBody] RequestEventAssigmentModelRequest requests, CancellationToken cancellationToken)
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

        var token = GetAccessTokenFromHeader();
        var userData = await identityClient.GetNameAsync(token, cancellationToken);

        await userService.AddOrUpdateUserAsync(userData);

        if (requests.Events?.Count > 0)
            await userService.SaveEventsAssigmentRequest(user, requests.Events);

        if (requests.Groups?.Count > 0)
        {
            var model = requests.Groups.Select(r => (r.Id, r.JoinedDate)).ToArray();
            await userService.SaveGroupsAssigmentRequests(user, model);
        }

        return Ok();
    }

    private string GetAccessTokenFromHeader()
    {
        var authToken = Request.Headers.Authorization.First();
        if (authToken == null)
            throw new AppException("Uuth token not found in headers.");

        if (authToken.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            authToken = authToken.Substring("Bearer ".Length);
        }

        return authToken;
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

    [HttpGet]
    [Route(ApiEndpoints.Video.Access.ListRequests)]
    public async Task<RequestedAccessesResponse> GetRequestAccessList()
    {
        var userId = User.GetSubject();

        var accessRequests = await userService.GetAccessRequestsAsync(userId);
        var response = ContractMappers.MapToAccessRequests(accessRequests);

        return response;
    }



}
