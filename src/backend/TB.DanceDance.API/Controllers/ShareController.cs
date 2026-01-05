using Domain.Services;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.API.Extensions;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class ShareController : Controller
{
    private readonly ISharedLinkService sharedLinkService;
    private readonly IVideoService videoService;
    private readonly IOptions<AppOptions> appOptions;
    private readonly ILogger<ShareController> logger;

    public ShareController(
        ISharedLinkService sharedLinkService,
        IVideoService videoService,
        IOptions<AppOptions> appOptions,
        ILogger<ShareController> logger)
    {
        this.sharedLinkService = sharedLinkService;
        this.videoService = videoService;
        this.appOptions = appOptions;
        this.logger = logger;
    }

    /// <summary>
    /// Creates a shared link for a video. Requires authentication.
    /// </summary>
    [HttpPost]
    [Route(ApiEndpoints.Share.Create)]
    public async Task<ActionResult<SharedLinkResponse>> CreateSharedLink(
        [FromRoute] Guid videoId,
        [FromBody] CreateSharedLinkRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        try
        {
            var link = await sharedLinkService.CreateSharedLinkAsync(
                videoId,
                userId,
                request.ExpirationDays,
                cancellationToken);

            var response = new SharedLinkResponse
            {
                LinkId = link.Id,
                VideoId = link.VideoId,
                VideoName = link.Video?.Name ?? string.Empty,
                CreatedAt = link.CreatedAt,
                ExpireAt = link.ExpireAt,
                IsRevoked = link.IsRevoked,
                ShareUrl = ResolveLinkUrl(link.Id)
            };

            return Ok(response);
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
    [Route(ApiEndpoints.Share.Revoke)]
    public async Task<IActionResult> RevokeSharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var result = await sharedLinkService.RevokeSharedLinkAsync(linkId, userId, cancellationToken);

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
    [Route(ApiEndpoints.Share.GetMy)]
    public async Task<ActionResult<IEnumerable<SharedLinkResponse>>> GetMySharedLinks(CancellationToken cancellationToken)
    {
        var userId = User.GetSubject();

        var links = await sharedLinkService.GetUserSharedLinksAsync(userId, cancellationToken);

        var response = links.Select(link => new SharedLinkResponse
        {
            LinkId = link.Id,
            VideoId = link.VideoId,
            VideoName = link.Video?.Name ?? string.Empty,
            CreatedAt = link.CreatedAt,
            ExpireAt = link.ExpireAt,
            IsRevoked = link.IsRevoked,
            ShareUrl = ResolveLinkUrl(link.Id)
        });

        return Ok(response);
    }

    private string ResolveLinkUrl(string linkId) => $"{this.appOptions.Value.AppWebsiteOrigin}/share/{linkId}";

    /// <summary>
    /// Gets video information by shared link ID. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route(ApiEndpoints.Share.GetInfo)]
    public async Task<ActionResult<SharedVideoInfoResponse>> GetVideoInfoBySharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var video = await sharedLinkService.GetVideoBySharedLinkAsync(linkId, cancellationToken);

        if (video == null)
        {
            return NotFound(new { error = "Shared link not found, expired, or revoked." });
        }

        var response = new SharedVideoInfoResponse
        {
            VideoId = video.Id,
            Name = video.Name,
            Duration = video.Duration,
            RecordedDateTime = video.RecordedDateTime
        };

        return Ok(response);
    }

    /// <summary>
    /// Streams a video by shared link ID. Anonymous access allowed.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route(ApiEndpoints.Share.GetStream)]
    public async Task<IActionResult> StreamVideoBySharedLink(
        [FromRoute] string linkId,
        CancellationToken cancellationToken)
    {
        var video = await sharedLinkService.GetVideoBySharedLinkAsync(linkId, cancellationToken);

        if (video == null)
        {
            return NotFound(new { error = "Shared link not found, expired, or revoked." });
        }

        if (string.IsNullOrEmpty(video.BlobId))
        {
            return NotFound(new { error = "Video file not available." });
        }

        var stream = await videoService.OpenStream(video.BlobId, cancellationToken);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }
}
