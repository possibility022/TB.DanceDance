using Application.Extensions;
using Application.Features.Videos;
using Domain.Entities;
using FastEndpoints;
using Void = FastEndpoints.Void;

namespace Application.Features.Transfers.Endpoints;

/// <summary>
/// Streams a transfer item's video so the recipient can preview it before deciding. Requires
/// authentication (JWT may arrive in the <c>token</c> query param so the &lt;video&gt; element can
/// play it). Valid only for a live, pending transfer and only for a would-be recipient (not the
/// sender, who streams via the normal video endpoints).
/// </summary>
public class StreamVideoByTransferEndpoint : EndpointWithoutRequest
{
    private readonly ITransferService transferService;
    private readonly IVideoService videoService;

    public StreamVideoByTransferEndpoint(ITransferService transferService, IVideoService videoService)
    {
        this.transferService = transferService;
        this.videoService = videoService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Transfer.GetStream);
        Policies(ApiScopes.Read);
    }

    public override async Task<Void> HandleAsync(CancellationToken ct)
    {
        var userId = User.TryGetSubject();
        if (string.IsNullOrWhiteSpace(userId))
            return await Send.UnauthorizedAsync(ct);

        var linkId = Route<string>("linkId") ?? string.Empty;
        var videoId = Route<Guid>("videoId");

        var transfer = await transferService.GetTransferAsync(linkId, ct);

        // Only a live, pending transfer is previewable, and only by a would-be recipient. Once
        // Accepted, the video is unconditionally the recipient's — they stream it via the normal
        // video endpoints instead.
        if (transfer == null || transfer.Status != TransferStatus.Pending || transfer.CreatedBy == userId)
            return await Send.NotFoundAsync(ct);

        var item = transfer.Items.FirstOrDefault(i => i.VideoId == videoId);
        if (item == null || string.IsNullOrEmpty(item.Video.BlobId))
            return await Send.NotFoundAsync(ct);

        var stream = await videoService.OpenStream(item.Video.BlobId, ct);
        return await Send.StreamAsync(stream, contentType: "video/mp4", enableRangeProcessing: true, cancellation: ct);
    }
}
