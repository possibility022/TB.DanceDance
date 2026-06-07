using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.AccessManagement;

namespace Application.Features.AccessManagement.Endpoints;

public class ApproveAccessRequestEndpoint : Endpoint<ApproveAccessRequestRequest>
{
    private readonly IAccessManagementService accessManagementService;

    public ApproveAccessRequestEndpoint(IAccessManagementService accessManagementService)
    {
        this.accessManagementService = accessManagementService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Access.ManageAccessRequests);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(ApproveAccessRequestRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        bool results;

        if (req.IsApproved)
            results = await accessManagementService.ApproveAccessRequest(req.RequestId, req.IsGroup, userId);
        else
            results = await accessManagementService.DeclineAccessRequest(req.RequestId, req.IsGroup, userId, ct);

        if (results)
            await Send.NoContentAsync(ct);
        else
            await Send.ErrorsAsync(cancellation: ct);
    }
}