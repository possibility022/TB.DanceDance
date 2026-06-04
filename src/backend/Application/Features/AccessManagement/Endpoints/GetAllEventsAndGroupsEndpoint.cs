using Application.Features.Events;
using Application.Features.Groups;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Models;
using Group = TB.DanceDance.API.Contracts.Models.Group;

namespace Application.Features.AccessManagement.Endpoints;

public record GetAllEventsAndGroupsResponse
{
    public required ICollection<Event> Events { get; init; }
    public required ICollection<Group> Groups { get; init; }
}

public class GetAllEventsAndGroupsEndpoint : EndpointWithoutRequest<GetAllEventsAndGroupsResponse>
{
    private readonly IEventService eventService;
    private readonly IGroupService groupService;

    public GetAllEventsAndGroupsEndpoint(IEventService eventService, IGroupService groupService)
    {
        this.eventService = eventService;
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Access.GetAll);
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var listOfEvents = await eventService.GetAllEvents(ct);
        var listOfGroups = await groupService.GetAllGroups(ct);

        var response = new GetAllEventsAndGroupsResponse
        {
            Events = listOfEvents.Select(ContractMappers.MapToEventContract).ToArray(),
            Groups = listOfGroups.Select(ContractMappers.MapToGroupContract).ToArray(),
        };

        await Send.OkAsync(response, ct);
    }
}
