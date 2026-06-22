using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class ListMyGroupsEndpoint : EndpointWithoutRequest<ListMyGroupsResponse>
{
    private readonly IGroupService groupService;

    public ListMyGroupsEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.My);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();

        var groups = await groupService.GetAdministeredGroupsAsync(userId, ct);
        await Send.OkAsync(new ListMyGroupsResponse { Groups = groups }, ct);
    }
}
