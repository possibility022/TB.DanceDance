using Application.Extensions;
using FastEndpoints;

namespace Application.Features.Groups.Endpoints;

public class RemoveGroupAdminEndpoint : EndpointWithoutRequest
{
    private readonly IGroupService groupService;

    public RemoveGroupAdminEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Delete(ApiRoutes.Groups.AdminById);
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

        var result = await groupService.RemoveAdminAsync(groupId, targetUserId!, ct);
        switch (result)
        {
            case RemoveGroupAdminResult.Removed:
                await Send.NoContentAsync(ct);
                break;
            case RemoveGroupAdminResult.NotAnAdmin:
                await Send.NotFoundAsync(ct);
                break;
            case RemoveGroupAdminResult.BlockedLastAdmin:
                AddError("A group must always have at least one admin.");
                await Send.ErrorsAsync(409, ct);
                break;
        }
    }
}
