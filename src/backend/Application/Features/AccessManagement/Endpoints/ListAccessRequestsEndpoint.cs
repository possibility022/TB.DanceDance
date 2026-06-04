using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.AccessManagement;

namespace Application.Features.AccessManagement.Endpoints;

public class GetAccessRequestsEndpoint : EndpointWithoutRequest<ListAccessRequestsResponse>
{
    private readonly IAccessManagementService accessManagementService;

    public GetAccessRequestsEndpoint(IAccessManagementService accessManagementService)
    {
        this.accessManagementService = accessManagementService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Access.ManageAccessRequests);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.GetSubject();

        var accessRequests = await accessManagementService.GetAccessRequestsToApproveAsync(userId, ct);

        var response = new ListAccessRequestsResponse
        {
            AccessRequests = accessRequests
                .Select(ContractMappers.MapToRequestedAccess)
                .ToArray(),
        };

        await Send.OkAsync(response, ct);
    }
}