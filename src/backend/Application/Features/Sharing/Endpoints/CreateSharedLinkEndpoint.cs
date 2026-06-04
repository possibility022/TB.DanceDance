using Application.Extensions;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLinkResponse = TB.DanceDance.API.Contracts.Features.Sharing.SharedLinkResponse;

namespace Application.Features.Sharing.Endpoints;

public record CreateSharedLinkRequest
{
    /// <summary>The video to share (bound from the route).</summary>
    public Guid VideoId { get; set; }

    /// <summary>Number of days until the link expires (1-365). Default 7.</summary>
    public int ExpirationDays { get; set; } = 7;

    /// <summary>Whether commenting is allowed through this link. Default true.</summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>Whether anonymous commenting is allowed through this link. Default false.</summary>
    public bool AllowAnonymousComments { get; set; } = false;
}

/// <summary>
/// Creates a shared link for a video. Requires authentication.
/// </summary>
public class CreateSharedLinkEndpoint : Endpoint<CreateSharedLinkRequest, SharedLinkResponse>
{
    private readonly ISharedLinkService sharedLinkService;
    private readonly IOptions<AppOptions> appOptions;

    public CreateSharedLinkEndpoint(ISharedLinkService sharedLinkService, IOptions<AppOptions> appOptions)
    {
        this.sharedLinkService = sharedLinkService;
        this.appOptions = appOptions;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Share.Create);
        Policies(ApiScopes.Read);
    }

    public override async Task HandleAsync(CreateSharedLinkRequest req, CancellationToken ct)
    {
        var userId = User.GetSubject();

        try
        {
            var link = await sharedLinkService.CreateSharedLinkAsync(
                req.VideoId,
                userId,
                req.ExpirationDays,
                req.AllowComments,
                req.AllowAnonymousComments,
                ct);

            var response = ShareMapper.MapToSharedLinkResponse(link, appOptions.Value.AppWebsiteOrigin);
            await Send.OkAsync(response, ct);
        }
        catch (ArgumentException ex)
        {
            Logger.LogWarning(ex, "Failed to create shared link for video {VideoId} by user {UserId}", req.VideoId, userId);
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
        }
    }
}
