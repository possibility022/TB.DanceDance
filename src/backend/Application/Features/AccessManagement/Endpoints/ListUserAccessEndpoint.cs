using Application.Extensions;
using Application.Features.Events;
using Application.Features.Groups;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.AccessManagement;
using TB.DanceDance.API.Contracts.Features.AccessManagement.Models;

namespace Application.Features.AccessManagement.Endpoints;

public class GetUserAccessEndpoint : EndpointWithoutRequest<GetUserAccessResponse>
{
    private readonly IAccessService accessService;
    private readonly IEventService eventService;
    private readonly IGroupService groupService;
    private readonly IAccessManagementService accessManagementService;

    public GetUserAccessEndpoint(
        IAccessService accessService,
        IEventService eventService,
        IGroupService groupService,
        IAccessManagementService accessManagementService)
    {
        this.accessService = accessService;
        this.eventService = eventService;
        this.groupService = groupService;
        this.accessManagementService = accessManagementService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Access.GetUserAccess);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = User.GetSubject();
        (var userGroups, var userEvents) = await accessService.GetUserEventsAndGroupsAsync(user, ct);

        var listOfEvents = await eventService.GetAllEvents(ct);
        var listOfGroups = await groupService.GetAllGroups(ct);
        var myPendingRequests = await accessManagementService.GetPendingUserRequests(user, ct);
        var administeredGroupIds = await groupService.GetAdministeredGroupIdsAsync(user, ct);

        var response = new GetUserAccessResponse
        {
            Assigned = new GetUserAccessSet
            {
                Groups = userGroups
                    .Select(ContractMappers.MapToGroupContract)
                    .ToArray(),
                Events = userEvents
                    .OrderByDescending(r => r.Date)
                    .Select(ContractMappers.MapToEventContract)
                    .ToArray(),
            },
            Available = new GetUserAccessSet
            {
                Events = listOfEvents.Except(userEvents)
                    .Select(ContractMappers.MapToEventContract)
                    .ToArray(),
                Groups = listOfGroups.Except(userGroups)
                    .Select(ContractMappers.MapToGroupContract)
                    .ToArray(),
            },
            Pending = new ListUserAccessPending
            {
                Events = myPendingRequests.Events,
                Groups = myPendingRequests.Groups,
            },
            AdministeredGroupIds = administeredGroupIds,
        };

        await Send.OkAsync(response, ct);
    }
}