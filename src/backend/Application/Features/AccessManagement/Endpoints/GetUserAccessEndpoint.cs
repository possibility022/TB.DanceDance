using Application.Extensions;
using Application.Features.Events;
using Application.Features.Groups;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Models;
using Group = TB.DanceDance.API.Contracts.Models.Group;

namespace Application.Features.AccessManagement.Endpoints;

public record GetUserAccessResponse
{
    public required GetUserAccessSet Assigned { get; init; }
    public required GetUserAccessSet Available { get; init; }
    public required GetUserAccessPending Pending { get; init; }
}

public record GetUserAccessSet
{
    public required ICollection<Event> Events { get; init; }
    public required ICollection<Group> Groups { get; init; }
}

public record GetUserAccessPending
{
    public required IReadOnlyCollection<Guid> Events { get; init; }
    public required IReadOnlyCollection<Guid> Groups { get; init; }
}

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
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = User.GetSubject();
        (var userGroups, var userEvents) = await accessService.GetUserEventsAndGroupsAsync(user, ct);

        var listOfEvents = await eventService.GetAllEvents(ct);
        var listOfGroups = await groupService.GetAllGroups(ct);
        var myPendingRequests = await accessManagementService.GetPendingUserRequests(user, ct);

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
            Pending = new GetUserAccessPending
            {
                Events = myPendingRequests.Events,
                Groups = myPendingRequests.Groups,
            },
        };

        await Send.OkAsync(response, ct);
    }
}
