using Application.Extensions;
using Domain.Exceptions;
using FastEndpoints;

namespace Application.Features.AccessManagement.Endpoints;

public class RequestAccessEndpoint : Endpoint<RequestAccessRequest>
{
    private readonly IAccessManagementService accessManagementService;
    private readonly IIdentityClient identityClient;

    public RequestAccessEndpoint(
        IAccessManagementService accessManagementService,
        IIdentityClient identityClient)
    {
        this.accessManagementService = accessManagementService;
        this.identityClient = identityClient;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Access.RequestAccess);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(RequestAccessRequest req, CancellationToken ct)
    {
        if (req.Events?.Any() != true && req.Groups?.Any() != true)
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        if (req.Groups != null && req.Groups.Any(r => r.JoinedDate == default))
        {
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var user = User.GetSubject();

        var token = GetAccessTokenFromHeader();
        var userData = await identityClient.GetNameAsync(token, ct);

        await accessManagementService.AddOrUpdateUserAsync(userData, ct);

        if (req.Events?.Count > 0)
            await accessManagementService.SaveEventsAssigmentRequest(user, req.Events, ct);

        if (req.Groups?.Count > 0)
        {
            var model = req.Groups.Select(r => (r.Id, r.JoinedDate)).ToArray();
            await accessManagementService.SaveGroupsAssigmentRequests(user, model, ct);
        }

        await Send.NoContentAsync(ct);
    }

    private string GetAccessTokenFromHeader()
    {
        var authToken = HttpContext.Request.Headers.Authorization.First();
        if (authToken == null)
            throw new AppException("Auth token not found in headers.");

        if (authToken.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            authToken = authToken.Substring("Bearer ".Length);
        }

        return authToken;
    }
}