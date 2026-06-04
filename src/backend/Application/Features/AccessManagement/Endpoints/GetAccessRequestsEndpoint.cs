using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Models;

namespace Application.Features.AccessManagement.Endpoints;

public record GetAccessRequestsResponse
{
    public required IReadOnlyCollection<RequestedAccessModel> AccessRequests { get; init; }
}

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
        // TODO: original [Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
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
