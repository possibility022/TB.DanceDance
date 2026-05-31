using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Access.Contracts;
using TB.DanceDance.API;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Utilities.Mediating;

namespace TB.DanceDance.API.Features.AccessManagement;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class AccessController : Controller
{
    private readonly IMediator mediator;
    private readonly IIdentityClient identityClient;

    public AccessController(IMediator mediator, IIdentityClient identityClient)
    {
        this.mediator = mediator;
        this.identityClient = identityClient;
    }

    [Route(AccessRoutes.GetAll)]
    [HttpGet]
    public async Task<EventsAndGroupsResponse> GetAllEventsAndGroups(CancellationToken token)
    {
        var listOfEvents = await mediator.SendAsync(new GetAllEventsQuery(), token);
        var listOfGroups = await mediator.SendAsync(new GetAllGroupsQuery(), token);

        return new EventsAndGroupsResponse()
        {
            Events = listOfEvents
                .Select(ContractMappers.MapToEventContract)
                .ToList(),
            Groups = listOfGroups
                .Select(ContractMappers.MapToGroupContract)
                .ToList()
        };
    }

    [Route(AccessRoutes.GetUserAccess)]
    public async Task<UserEventsAndGroupsResponse> GetAssignedGroupsAsync(CancellationToken cancellationToken)
    {
        var user = User.GetSubject();
        var assigned = await mediator.SendAsync(new GetUserGroupsAndEvents { UserId = user }, cancellationToken);
        var userGroups = assigned.Groups;
        var userEvents = assigned.Events;

        var responseModel = new UserEventsAndGroupsResponse();

        responseModel.Assigned.Groups = userGroups
            .Select(ContractMappers.MapToGroupContract)
                .ToArray();

        responseModel.Assigned.Events = userEvents
                .OrderByDescending(r => r.Date)
                .Select(ContractMappers.MapToEventContract)
                .ToArray();

        var listOfEvents = await mediator.SendAsync(new GetAllEventsQuery(), cancellationToken);

        responseModel.Available.Events = listOfEvents.Except(userEvents)
            .Select(ContractMappers.MapToEventContract)
            .ToArray();

        var listOfGroups = await mediator.SendAsync(new GetAllGroupsQuery(), cancellationToken);

        responseModel.Available.Groups = listOfGroups.Except(userGroups)
            .Select(ContractMappers.MapToGroupContract)
            .ToArray();

        var myPendingRequests = await mediator.SendAsync(new GetPendingUserRequestsQuery { UserId = user }, cancellationToken);

        responseModel.Pending.Events = myPendingRequests.Events;
        responseModel.Pending.Groups = myPendingRequests.Groups;

        return responseModel;
    }

    [Route(AccessRoutes.RequestAccess)]
    [HttpPost]
    public async Task<IActionResult> RequestAccess([FromBody] RequestAssigmentModelRequest requests, CancellationToken cancellationToken)
    {
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

        await mediator.SendAsync(new AddOrUpdateUserCommand
        {
            Id = userData.Id,
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            Email = userData.Email,
        }, cancellationToken);

        if (requests.Events?.Count > 0)
            await mediator.SendAsync(new SaveEventsAssignmentCommand { UserId = user, Events = requests.Events }, cancellationToken);

        if (requests.Groups?.Count > 0)
        {
            var model = requests.Groups.Select(r => (r.Id, r.JoinedDate)).ToArray();
            await mediator.SendAsync(new SaveGroupsAssignmentCommand { UserId = user, Groups = model }, cancellationToken);
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
    [Route(AccessRoutes.ManageAccessRequests)]
    public async Task<RequestedAccessesResponse> GetRequestAccessList(CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var accessRequests = await mediator.SendAsync(new GetAccessRequestsToApproveQuery { UserId = userId }, cancellationToken);
        var response = ContractMappers.MapToAccessRequests(accessRequests);

        return response;
    }

    [HttpPost]
    [Route(AccessRoutes.ManageAccessRequests)]
    public async Task<IActionResult> ApproveOrRejectRequestAccess([FromBody] ApproveAccessRequest requestBody, CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        bool results;

        if (requestBody.IsApproved)
            results = await mediator.SendAsync(new ApproveAccessRequestCommand
            {
                RequestId = requestBody.RequestId,
                IsGroup = requestBody.IsGroup,
                UserId = userId,
            }, cancellationToken);
        else
            results = await mediator.SendAsync(new DeclineAccessRequestCommand
            {
                RequestId = requestBody.RequestId,
                IsGroup = requestBody.IsGroup,
                UserId = userId,
            }, cancellationToken);

        if (results)
            return Ok();
        else
            return BadRequest();
    }
}
