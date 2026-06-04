using Application.Extensions;
using FastEndpoints;

namespace Application.Features.AccessManagement.Endpoints;

public record ApproveAccessRequestRequest
{
    public Guid RequestId { get; set; }
    public bool IsGroup { get; set; }
    public bool IsApproved { get; set; }
}

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
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
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
            await Send.OkAsync(ct);
        else
            await Send.ErrorsAsync(cancellation: ct);
    }
}
