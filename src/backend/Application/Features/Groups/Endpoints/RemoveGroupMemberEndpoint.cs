using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Groups.Endpoints;

public class RemoveGroupMemberEndpoint : EndpointWithoutRequest
{
    private readonly IGroupService groupService;

    public RemoveGroupMemberEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Groups.MemberById);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var targetUserId = Route<string>("userId");
        var userId = User.GetSubject();

        if (!await groupService.IsGroupAdmin(groupId, userId, ct))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var removed = await groupService.RemoveMemberAsync(groupId, targetUserId!, ct);
        if (!removed)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
