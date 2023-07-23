﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Data.PostgreSQL.Models;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

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
    public EventsAndGroupsResponse GetAssignedGroups()
    {
        var user = User.GetSubject();
        (var groups, var evenets) = userService.GetUserEventsAndGroups(user);

        return new EventsAndGroupsResponse()
        {
            Groups = groups
            .Select(group => ContractMappers.MapToGroupContract(group))
                .ToList(),
            Events = evenets
                .Select(@event => ContractMappers.MapToEventContract(@event))
                .ToList()
        };
    }

    [HttpPost]
    [Route(ApiEndpoints.Event.AddEvent)]
    public async Task<IActionResult> CreateEventAsync(CreateNewEventRequest request)
    {
        var @event = ContractMappers.MapFromNewEventRequestToEvent(request);
        var user = User.GetSubject();


        if (!ModelState.IsValid)
            return BadRequest();

        await eventService.CreateEventAsync(@event, user);


        return Ok();
    }

    [Route(ApiEndpoints.Video.Access.RequestAccess)]
    [HttpPost]
    public async Task<IActionResult> RequestAssigment([FromBody] RequestEventAssigmentModelRequest requests)
    {
        if (requests == null)
            return BadRequest();

        var user = User.GetSubject();

        var tasks = new Task[] { Task.CompletedTask, Task.CompletedTask };

        if (requests.Events?.Count > 0)
            tasks[0] = userService.SaveEventsAssigmentRequest(user, requests.Events);

        if (requests.Groups?.Count > 0)
            tasks[1] = userService.SaveGroupsAssigmentRequests(user, requests.Groups);

        await Task.WhenAll(tasks);

        return Ok();
    }
}
