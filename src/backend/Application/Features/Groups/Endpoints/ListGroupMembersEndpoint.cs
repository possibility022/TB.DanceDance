using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class ListGroupMembersEndpoint : EndpointWithoutRequest<ListGroupMembersResponse>
{
    private readonly IGroupService groupService;

    public ListGroupMembersEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.Members);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var userId = User.GetSubject();

        if (!await groupService.IsGroupAdmin(groupId, userId, ct))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var members = await groupService.GetMembersAsync(groupId, ct);
        await Send.OkAsync(new ListGroupMembersResponse { Members = members }, ct);
    }
}
