using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public class RenameVideoEndpoint : Endpoint<RenameVideoRequest, EmptyResponse>
{
    private readonly IAccessService accessService;
    private readonly IVideoService videoService;

    public RenameVideoEndpoint(IAccessService accessService, IVideoService videoService)
    {
        this.accessService = accessService;
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Video.Rename);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(RenameVideoRequest req, CancellationToken ct)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(req.VideoId, User.GetSubject(), ct);
        if (!hasAccess)
            return await Send.UnauthorizedAsync(ct);
        var res = await videoService.RenameVideoAsync(req.VideoId, req.NewName, ct);

        if (res == false)
            return await Send.ErrorsAsync(cancellation: ct);

        return await Send.NoContentAsync(ct);
    }
}