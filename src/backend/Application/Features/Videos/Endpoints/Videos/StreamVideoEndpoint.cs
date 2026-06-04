using Application.Extensions;
using Application.Features.AccessManagement;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Videos;

public record StreamVideoRequest(string BlobId);

public class StreamVideoEndpoint : Endpoint<StreamVideoRequest, EmptyResponse>
{
    private readonly IAccessService accessService;
    private readonly IVideoService videoService;

    public StreamVideoEndpoint(IAccessService accessService, IVideoService videoService)
    {
        this.accessService = accessService;
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Video.GetStream);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(StreamVideoRequest req, CancellationToken ct)
    {
        var userSubjectId = User.TryGetSubject();
        if (string.IsNullOrWhiteSpace(userSubjectId))
            return await Send.UnauthorizedAsync(ct);

        // TODO: Implement storage quota enforcement for private videos at VIEW/STREAM time
        // Check if this is a private video (SharedWith.EventId == null && SharedWith.GroupId == null)
        // If private:
        //   1. Calculate user's total private video ConvertedBlobSize
        //   2. Compare against User.StorageQuotaBytes
        //   3. If over quota, return 403 Forbidden with message about storage limit
        // This allows users to upload videos even over quota, but they cannot view them until space is freed

        var hasAccess = await accessService.DoesUserHasAccessAsync(req.BlobId, userSubjectId, ct);
        if (!hasAccess)
            return await Send.UnauthorizedAsync(ct);

        var stream = await videoService.OpenStream(req.BlobId, ct);
        return await Send.StreamAsync(stream, contentType: "video/mp4", cancellation: ct);
    }
}