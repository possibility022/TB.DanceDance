using Application.Extensions;
using Application.Features.AccessManagement;
using Application.Features.Videos.Models;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public record RefreshUploadUrlRequest
{
    public Guid VideoId { get; init; }
}

public record RefreshUploadUrlResponse : UploadUrlResponse
{
}

public class RefreshUploadUrlEndpoint : Endpoint<RefreshUploadUrlRequest, RefreshUploadUrlResponse>
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
    }

    public override async Task<Void> HandleAsync(RefreshUploadUrlRequest req, CancellationToken ct)
    {
        string user  = User.GetSubject();
        var hasAccess = await accessService.DoesUserHasAccessAsync(req.VideoId, user, ct);
        if (!hasAccess)
            return await Send.UnauthorizedAsync(ct);

        var sharedBlob = await videoService.GetSharingLink(req.VideoId, ct);
        
        if (sharedBlob == null)
            return await Send.NotFoundAsync(ct);

        return await Send.OkAsync(
            new RefreshUploadUrlResponse()
            {
                Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
            }, ct);
    }
}