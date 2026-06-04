using Application.Extensions;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Sharing;

namespace Application.Features.Sharing.Endpoints
{
    /// <summary>
    /// Revokes a shared link. Requires authentication. Only the link creator or video owner can revoke.
    /// </summary>
    public class RevokeSharedLinkEndpoint : Endpoint<RevokeSharedLinkRequest>
    {
        private readonly ISharedLinkService sharedLinkService;

        public RevokeSharedLinkEndpoint(ISharedLinkService sharedLinkService)
        {
            this.sharedLinkService = sharedLinkService;
        }

        public override void Configure()
        {
            Delete(ApiRoutes.Share.Revoke);
            Policies(ApiScopes.Read);
        }

        public override async Task HandleAsync(RevokeSharedLinkRequest req, CancellationToken ct)
        {
            var userId = User.GetSubject();

            var result = await sharedLinkService.RevokeSharedLinkAsync(req.LinkId, userId, ct);

            if (!result)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.NoContentAsync(ct);
        }
    }
}
