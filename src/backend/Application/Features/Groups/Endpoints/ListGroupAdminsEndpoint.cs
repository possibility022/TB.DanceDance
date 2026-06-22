using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class ListGroupAdminsEndpoint : EndpointWithoutRequest<ListGroupAdminsResponse>
{
    private readonly IGroupService groupService;

    public ListGroupAdminsEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Groups.Admins);
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

        var admins = await groupService.GetAdminsAsync(groupId, ct);
        await Send.OkAsync(new ListGroupAdminsResponse { Admins = admins }, ct);
    }
}
