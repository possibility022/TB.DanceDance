using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Features.Sharing;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.API.Features.Sharing;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class ShareController : Controller
{
    private readonly IRequestHandler<CreateSharedLinkCommand, SharedLinkDto> createSharedLinkCommand;
    private readonly IRequestHandler<RevokeSharedLinkCommand, bool> revokeSharedLinkCommand;
    private readonly IRequestHandler<GetUserSharedLinksQuery, IReadOnlyCollection<SharedLinkDto>> getUserSharedLinksQuery;
    private readonly IRequestHandler<GetSharedLinkQuery, SharedLinkDto?> getSharedLinkQuery;
    private readonly IRequestHandler<OpenVideoStreamQuery, Stream> openVideoStreamQuery;
    private readonly IRequestHandler<GetVideoBySharedLinkQuery, VideoDto?> getVideoBySharedLinkQuery;
    private readonly IOptions<AppOptions> appOptions;
    private readonly ILogger<ShareController> logger;

    public ShareController(
        IRequestHandler<CreateSharedLinkCommand, SharedLinkDto> createSharedLinkCommand,
        IRequestHandler<RevokeSharedLinkCommand, bool> revokeSharedLinkCommand,
        IRequestHandler<GetUserSharedLinksQuery, IReadOnlyCollection<SharedLinkDto>> getUserSharedLinksQuery,
        IRequestHandler<GetSharedLinkQuery, SharedLinkDto?> getSharedLinkQuery,
        IRequestHandler<OpenVideoStreamQuery, Stream> openVideoStreamQuery,
        IRequestHandler<GetVideoBySharedLinkQuery, VideoDto?> getVideoBySharedLinkQuery,
        IOptions<AppOptions> appOptions,
        ILogger<ShareController> logger)
    {
        this.createSharedLinkCommand = createSharedLinkCommand;
        this.revokeSharedLinkCommand = revokeSharedLinkCommand;
        this.getUserSharedLinksQuery = getUserSharedLinksQuery;
        this.getSharedLinkQuery = getSharedLinkQuery;
        this.openVideoStreamQuery = openVideoStreamQuery;
        this.getVideoBySharedLinkQuery = getVideoBySharedLinkQuery;
        this.appOptions = appOptions;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a shared link for a video. Requires authentication.
    /// </summary>
    [HttpPost]
    [Route(ShareRoutes.Create)]
    public async Task<ActionResult<SharedLinkResponse>> CreateSharedLink(
        [FromRoute] Guid videoId,
        [FromBody] CreateSharedLinkRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        try
        {
            var link = await createSharedLinkCommand.HandleAsync(new CreateSharedLinkCommand
            {
                VideoId = videoId,
                UserId = userId,
                ExpirationDays = request.ExpirationDays,
                AllowComments = request.AllowComments,
                AllowAnonymousComments = request.AllowAnonymousComments,
            }, cancellationToken);

            return Ok(MapToResponse(link));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed to create shared link for video {VideoId} by user {UserId}", videoId, userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revokes a shared link. Requires authentication. Only the link creator or video owner can revoke.
    /// </summary>
    [HttpDelete]
    [Route(ShareRoutes.Revoke)]
    public async Task<IActionResult> RevokeSharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var result = await revokeSharedLinkCommand.HandleAsync(new RevokeSharedLinkCommand(linkId, userId), cancellationToken);

        if (!result)
        {
            return NotFound(new { error = "Link not found or you are not authorized to revoke it." });
        }

        return Ok();
    }

    /// <summary>
    /// Gets all shared links created by the user or for videos owned by the user. Requires authentication.
    /// </summary>
    [HttpGet]
    [Route(ShareRoutes.GetMy)]
    public async Task<ActionResult<IEnumerable<SharedLinkResponse>>> GetMySharedLinks(CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var links = await getUserSharedLinksQuery.HandleAsync(new GetUserSharedLinksQuery(userId), cancellationToken);

        var response = links.Select(MapToResponse);

        return Ok(response);
    }

    private string ResolveLinkUrl(string linkId) => $"{this.appOptions.Value.AppWebsiteOrigin}/shared/{linkId}";

    private SharedLinkResponse MapToResponse(SharedLinkDto link) => new()
    {
        LinkId = link.Id,
        VideoId = link.VideoId,
        VideoName = link.Video?.Name ?? string.Empty,
        CreatedAt = link.CreatedAt,
        ExpireAt = link.ExpireAt,
        IsRevoked = link.IsRevoked,
        ShareUrl = ResolveLinkUrl(link.Id),
        AllowComments = link.AllowComments,
        AllowAnonymousComments = link.AllowAnonymousComments
    };

    /// <summary>
    /// Gets video information by shared link ID. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route(ShareRoutes.GetInfo)]
    public async Task<ActionResult<SharedVideoInfoResponse>> GetVideoInfoBySharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var link = await getSharedLinkQuery.HandleAsync(new GetSharedLinkQuery(linkId), cancellationToken);

        if (link == null || link.Video == null)
        {
            return NotFound(new { error = "Shared link not found, expired, or revoked." });
        }

        var video = link.Video;

        var response = new SharedVideoInfoResponse
        {
            VideoId = video.Id,
            Name = video.Name,
            Duration = video.Duration,
            RecordedDateTime = video.RecordedDateTime,
            CommentVisibility = video.CommentVisibility,
            AllowCommentsOnThisLink = link.AllowComments,
            AllowAnonymousCommentsOnThisLink = link.AllowAnonymousComments
        };

        return Ok(response);
    }

    /// <summary>
    /// Streams a video by shared link ID. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route(ShareRoutes.GetStream)]
    public async Task<IActionResult> StreamVideoBySharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var video = await getVideoBySharedLinkQuery.HandleAsync(new GetVideoBySharedLinkQuery(linkId), cancellationToken);

        if (video == null)
        {
            return NotFound(new { error = "Shared link not found, expired, or revoked." });
        }

        if (string.IsNullOrEmpty(video.BlobId))
        {
            return NotFound(new { error = "Video file not available." });
        }

        var stream = await openVideoStreamQuery.HandleAsync(new OpenVideoStreamQuery(video.BlobId), cancellationToken);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }
}
