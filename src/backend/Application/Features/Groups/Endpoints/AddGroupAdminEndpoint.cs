using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Groups;

namespace Application.Features.Groups.Endpoints;

public class AddGroupAdminEndpoint : Endpoint<AddGroupAdminRequest>
{
    private readonly IGroupService groupService;

    public AddGroupAdminEndpoint(IGroupService groupService)
    {
        this.groupService = groupService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Groups.Admins);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(AddGroupAdminRequest req, CancellationToken ct)
    {
        var groupId = Route<Guid>("groupId");
        var userId = User.GetSubject();

        if (!await groupService.IsGroupAdmin(groupId, userId, ct))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var added = await groupService.AddAdminAsync(groupId, req.UserId, ct);
        if (!added)
        {
            AddError(r => r.UserId, "User not found.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
