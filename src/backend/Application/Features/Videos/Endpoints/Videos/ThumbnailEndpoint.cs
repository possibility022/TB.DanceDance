using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public class ThumbnailEndpoint : EndpointWithoutRequest
{
    private readonly IAccessService accessService;
    private readonly IVideoService videoService;

    public ThumbnailEndpoint(IAccessService accessService, IVideoService videoService)
    {
        this.accessService = accessService;
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.GetThumbnail);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var userSubjectId = User.TryGetSubject();
        if (string.IsNullOrWhiteSpace(userSubjectId))
            return await Send.UnauthorizedAsync(ct);

        var blobId = Route<string>("blobId") ?? string.Empty;

        var hasAccess = await accessService.DoesUserHasAccessAsync(blobId, userSubjectId, ct);
        if (!hasAccess)
            return await Send.UnauthorizedAsync(ct);

        var stream = await videoService.OpenThumbnailStream(blobId, ct);
        if (stream == null)
            return await Send.NotFoundAsync(ct);

        return await Send.StreamAsync(stream, contentType: "image/jpeg", cancellation: ct);
    }
}
