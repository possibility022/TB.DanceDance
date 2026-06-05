using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public class RefreshUploadUrlEndpoint : EndpointWithoutRequest<RefreshUploadUrlResponse>
{
    private readonly IAccessService accessService;
    private readonly IVideoService videoService;

    public RefreshUploadUrlEndpoint(IAccessService accessService, IVideoService videoService)
    {
        this.accessService = accessService;
        this.videoService = videoService;
    }
    
    public override void Configure()
    {
        Get(ApiRoutes.Video.RefreshUploadUrl);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var videoId = Route<Guid>("videoId");
        
        string user  = User.GetSubject();
        var hasAccess = await accessService.DoesUserHasAccessAsync(videoId, user, ct);
        if (!hasAccess)
            return await Send.UnauthorizedAsync(ct);

        var sharedBlob = await videoService.GetSharingLink(videoId, ct);
        
        if (sharedBlob == null)
            return await Send.NotFoundAsync(ct);

        return await Send.OkAsync(
            new RefreshUploadUrlResponse()
            {
                Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
            }, ct);
    }
}