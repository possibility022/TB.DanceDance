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
    private readonly IIdentityClient identityClient;
    private readonly IRequestHandler<GetAllEventsQuery, IReadOnlyCollection<EventDto>> getAllEventsQuery;
    private readonly IRequestHandler<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>> getAllGroupsQuery;
    private readonly IRequestHandler<GetUserGroupsAndEvents, UserGroupsAndEvents> getUserGroupsAndEvents;
    private readonly IRequestHandler<GetPendingUserRequestsQuery, UserRequests> getPendingUserRequestsQuery;
    private readonly IRequestHandler<AddOrUpdateUserCommand, bool> addOrUpdateUserCommand;
    private readonly IRequestHandler<SaveGroupsAssignmentCommand, bool> saveGroupsAssignmentCommand;
    private readonly IRequestHandler<SaveEventsAssignmentCommand, bool> saveEventsAssignmentCommand;
    private readonly IRequestHandler<GetAccessRequestsToApproveQuery, IReadOnlyCollection<RequestedAccess>> getAccessRequestsToApproveQuery;
    private readonly IRequestHandler<DeclineAccessRequestCommand, bool> declineAccessRequestCommand;
    private readonly IRequestHandler<ApproveAccessRequestCommand, bool> approveAccessRequestCommand;

    public AccessController(IIdentityClient identityClient,
        IRequestHandler<GetAllEventsQuery, IReadOnlyCollection<EventDto>> getAllEventsQuery,
        IRequestHandler<GetAllGroupsQuery, IReadOnlyCollection<GroupDto>> getAllGroupsQuery,
        IRequestHandler<GetUserGroupsAndEvents, UserGroupsAndEvents> getUserGroupsAndEvents,
        IRequestHandler<GetPendingUserRequestsQuery,UserRequests> getPendingUserRequestsQuery,
        IRequestHandler<AddOrUpdateUserCommand, bool> addOrUpdateUserCommand,
        IRequestHandler<SaveGroupsAssignmentCommand, bool> saveGroupsAssignmentCommand,
        IRequestHandler<SaveEventsAssignmentCommand, bool> saveEventsAssignmentCommand,
        IRequestHandler<GetAccessRequestsToApproveQuery,IReadOnlyCollection<RequestedAccess>> getAccessRequestsToApproveQuery,
        IRequestHandler<DeclineAccessRequestCommand, bool> declineAccessRequestCommand,
        IRequestHandler<ApproveAccessRequestCommand, bool> approveAccessRequestCommand)
    {
        this.identityClient = identityClient;
        this.getAllEventsQuery = getAllEventsQuery;
        this.getAllGroupsQuery = getAllGroupsQuery;
        this.getUserGroupsAndEvents = getUserGroupsAndEvents;
        this.getPendingUserRequestsQuery = getPendingUserRequestsQuery;
        this.addOrUpdateUserCommand = addOrUpdateUserCommand;
        this.saveGroupsAssignmentCommand = saveGroupsAssignmentCommand;
        this.saveEventsAssignmentCommand = saveEventsAssignmentCommand;
        this.getAccessRequestsToApproveQuery = getAccessRequestsToApproveQuery;
        this.declineAccessRequestCommand = declineAccessRequestCommand;
        this.approveAccessRequestCommand = approveAccessRequestCommand;
    }

    [Route(AccessRoutes.GetAll)]
    [HttpGet]
    public async Task<EventsAndGroupsResponse> GetAllEventsAndGroups(CancellationToken token)
    {
        var listOfEvents = await getAllEventsQuery.HandleAsync(new GetAllEventsQuery(), token);
        var listOfGroups = await getAllGroupsQuery.HandleAsync(new GetAllGroupsQuery(), token);

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
        var assigned = await getUserGroupsAndEvents.HandleAsync(new GetUserGroupsAndEvents { UserId = user }, cancellationToken);
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

        var listOfEvents = await getAllEventsQuery.HandleAsync(new GetAllEventsQuery(), cancellationToken);

        responseModel.Available.Events = listOfEvents.Except(userEvents)
            .Select(ContractMappers.MapToEventContract)
            .ToArray();

        var listOfGroups = await getAllGroupsQuery.HandleAsync(new GetAllGroupsQuery(), cancellationToken);

        responseModel.Available.Groups = listOfGroups.Except(userGroups)
            .Select(ContractMappers.MapToGroupContract)
            .ToArray();

        var myPendingRequests = await getPendingUserRequestsQuery.HandleAsync(new GetPendingUserRequestsQuery { UserId = user }, cancellationToken);

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

        await addOrUpdateUserCommand.HandleAsync(new AddOrUpdateUserCommand
        {
            Id = userData.Id,
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            Email = userData.Email,
        }, cancellationToken);

        if (requests.Events?.Count > 0)
            await saveEventsAssignmentCommand.HandleAsync(new SaveEventsAssignmentCommand { UserId = user, Events = requests.Events }, cancellationToken);

        if (requests.Groups?.Count > 0)
        {
            var model = requests.Groups.Select(r => (r.Id, r.JoinedDate)).ToArray();
            await saveGroupsAssignmentCommand.HandleAsync(new SaveGroupsAssignmentCommand { UserId = user, Groups = model }, cancellationToken);
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

        var accessRequests = await getAccessRequestsToApproveQuery.HandleAsync(new GetAccessRequestsToApproveQuery { UserId = userId }, cancellationToken);
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
            results = await approveAccessRequestCommand.HandleAsync(new ApproveAccessRequestCommand
            {
                RequestId = requestBody.RequestId,
                IsGroup = requestBody.IsGroup,
                UserId = userId,
            }, cancellationToken);
        else
            results = await declineAccessRequestCommand.HandleAsync(new DeclineAccessRequestCommand
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
