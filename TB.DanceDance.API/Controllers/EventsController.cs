using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Models;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class EventsController : Controller
{
    private readonly IUserService userService;

    public EventsController(IUserService userService)
    {
        this.userService = userService;
    }

    [Route(ApiEndpoints.Video.Access.GetAll)]
    [HttpGet]
    public async Task<EventsAndGroups> GetAllEventsAndGroups()
    {
        var listOfEvents = await userService.GetAllEvents();
        var listOfGroups = await userService.GetAllGroups();

        return new EventsAndGroups()
        {
            Events = listOfEvents,
            Groups = listOfGroups
        };
    }

    [Route(ApiEndpoints.Video.Access.GetUserAccess)]
    public EventsAndGroups GetAssignedGroups()
    {
        var user = User.GetSubject();
        (var groups, var evenets) = userService.GetUserEventsAndGroups(user);

        return new EventsAndGroups()
        {
            Groups = groups,
            Events = evenets
        };
    }

    [Route(ApiEndpoints.Video.Access.RequestAccess)]
    [HttpPost]
    public async Task<IActionResult> RequestAssigment([FromBody] RequestEventAssigmentModel requests)
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
