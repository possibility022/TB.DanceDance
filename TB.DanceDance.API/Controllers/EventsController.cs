using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Models;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers
{
    [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
    public class EventsController : Controller
    {
        private readonly IUserService userService;

        public EventsController(IUserService userService)
        {
            this.userService = userService;
        }

        [Route("api/events/getall")]
        [HttpGet]
        public async Task<EventsAndGroups> GetInformationsAsync()
        {
            var user = User.GetSubject();
            var listOfEvents = await userService.GetAllEvents(user);
            var listOfGroups = await userService.GetAllGroups(user);


            return new EventsAndGroups()
            {

                Events = listOfEvents.Select(r => new EventSharingSharingScope()
                {
                    Assignment = AssignmentType.Event,
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.EventType
                }).ToList(),

                Groups = listOfGroups.Select(r => new SharingScopeModel()
                {
                    Assignment = AssignmentType.Group,
                    Id = r.Id,
                    Name = r.GroupName
                }).ToList()
            };
        }

        [Route("api/events/requestassigment")]
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
}
