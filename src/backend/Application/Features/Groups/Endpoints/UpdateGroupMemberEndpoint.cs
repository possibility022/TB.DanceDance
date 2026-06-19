using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class UpdateGroupMemberEndpoint : Endpoint<UpdateGroupMemberRequest>
{
    private readonly IGroupService groupService;

    public UpdateGroupMemberEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Put(ApiRoutes.Groups.MemberById);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(UpdateGroupMemberRequest req, CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var targetUserId = Route<string>("userId");
        var userId = User.GetSubject();

        if (!await groupService.IsGroupAdmin(groupId, userId, ct))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var updated = await groupService.UpdateMemberJoinedAsync(groupId, targetUserId!, req.WhenJoined.ToUniversalTime(), ct);
        if (!updated)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
