using Application.Extensions;
using FastEndpoints;

namespace Application.Features.AccessManagement.Endpoints;

public class GetAccessRequestsEndpoint : EndpointWithoutRequest<GetAccessRequestsResponse>
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

        var response = new GetAccessRequestsResponse
        {
            AccessRequests = accessRequests
                .Select(ContractMappers.MapToRequestedAccess)
                .ToArray(),
        };

        await Send.OkAsync(response, ct);
    }
}