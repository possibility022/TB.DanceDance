using Domain.Services;
using IdentityServer4.Validation;
using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class VideoController : Controller
{
    public VideoController(IVideoService videoService,
        ITokenValidator tokenValidator,
        IAccessService accessService,
        ILogger<VideoController> logger)
    {
        this.videoService = videoService;
        this.tokenValidator = tokenValidator;
        this.accessService = accessService;
        this.logger = logger;
    }

    private readonly IVideoService videoService;
    private readonly ITokenValidator tokenValidator;
    private readonly IAccessService accessService;
    private readonly ILogger<VideoController> logger;


    [Route(ApiEndpoints.Video.MyVideos)]
    public async Task<IActionResult> MyVideos(CancellationToken cancellationToken)
    {
        var videos = await accessService.GetUserPrivateVideos(User.GetSubject(), cancellationToken);
        var apiModel = videos.Select(ContractMappers.MapToVideoInformation);
        
        return new OkObjectResult(apiModel);
    }
    
    [Route(ApiEndpoints.Video.GetSingle)]
    [HttpGet]
    public async Task<IActionResult> GetInformationAsync(string guid, CancellationToken cancellationToken)
    {
        string user = User.GetSubject();

        var info = await videoService.GetVideoByBlobAsync(user, guid, cancellationToken);

        if (info == null)
            return NotFound();

        var results = ContractMappers.MapToVideoInformation(info);

        return new OkObjectResult(results);
    }

    [Route(ApiEndpoints.Video.GetStream)]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStreamAsync(string guid, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        // todo create better authentication. Send send tokens in headers

        if (string.IsNullOrEmpty(token) && Request.Headers.TryGetValue("Authorization", out var tokenFromHeader))
        {
            token = tokenFromHeader.FirstOrDefault()?.Substring("Bearer ".Length);
        }

        var validationRes = await tokenValidator.ValidateAccessTokenAsync(token);
        if (validationRes == null)
            // Idk when this can happen
            throw new Exception("Results of validation are null.");

        if (validationRes.IsError)
            return Unauthorized();

        var userSubjectId = validationRes.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (userSubjectId == null)
            return BadRequest();

        // TODO: Implement storage quota enforcement for private videos at VIEW/STREAM time
        // Check if this is a private video (SharedWith.EventId == null && SharedWith.GroupId == null)
        // If private:
        //   1. Calculate user's total private video ConvertedBlobSize
        //   2. Compare against User.StorageQuotaBytes
        //   3. If over quota, return 403 Forbidden with message about storage limit
        // This allows users to upload videos even over quota, but they cannot view them until space is freed

        var hasAccess = await accessService.DoesUserHasAccessAsync(guid, userSubjectId, cancellationToken);
        if (!hasAccess)
            return new UnauthorizedResult();

        var stream = await videoService.OpenStream(guid, cancellationToken);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }

    [Route(ApiEndpoints.Video.Rename)]
    [HttpPost]
    public async Task<IActionResult> RenameVideo([FromRoute] Guid videoId, [FromBody] VideoRenameRequest input, CancellationToken cancellationToken)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(videoId, User.GetSubject(), cancellationToken);
        if (!hasAccess)
            return Unauthorized();
        var res = await videoService.RenameVideoAsync(videoId, input.NewName, cancellationToken);

        if (res == false)
            return BadRequest();

        return Ok();
    }

    [HttpGet]
    [Route(ApiEndpoints.Video.RefreshUploadUrl)]
    public async Task<ActionResult<UploadVideoInformationResponse>> GetUploadInformation([FromRoute]Guid videoId, CancellationToken cancellationToken)
    {
        string user  = User.GetSubject();
        var hasAccess = await accessService.DoesUserHasAccessAsync(videoId, user, cancellationToken);
        if (!hasAccess)
            return Unauthorized();

        var sharedBlob = await videoService.GetSharingLink(videoId, cancellationToken);
        
        if (sharedBlob == null)
            return NotFound();
        
        return new UploadVideoInformationResponse()
        {
            Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
        };
    }

    [Route(ApiEndpoints.Video.GetUploadUrl)]
    [HttpPost]
    public async Task<ActionResult<UploadVideoInformationResponse>> GetUploadInformation(
        [FromBody] SharedVideoInformationRequest sharedVideoInformation,
        CancellationToken cancellationToken
        )
    {
        string? user = null;
        var sharedWith = sharedVideoInformation?.SharedWith;

        if (sharedVideoInformation == null)
        {
            return BadRequest("SharedVideoInformation is required.");
        }

        // TODO: Storage quota is enforced at VIEW/STREAM time, not upload time
        // Users can always upload videos regardless of quota status
        // Quota enforcement happens when trying to view/stream the video
        // This allows users to upload first, then manage storage by deleting old videos if needed

        user = User.GetSubject();

        // Handle different sharing types
        if (sharedVideoInformation.SharingWithType == SharingWithType.Group)
        {
            if (sharedWith == null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Group sharing type.");
                return BadRequest(ModelState);
            }

            var canUploadToGroup = await accessService.CanUserUploadToGroupAsync(user, sharedWith.Value, cancellationToken);

            if (!canUploadToGroup)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return new UnauthorizedResult();
            }
        }
        else if (sharedVideoInformation.SharingWithType == SharingWithType.Event)
        {
            if (sharedWith == null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith is required for Event sharing type.");
                return BadRequest(ModelState);
            }

            var canUploadToEvent = await accessService.CanUserUploadToEventAsync(user, sharedWith.Value, cancellationToken);

            if (!canUploadToEvent)
            {
                logger.LogWarning(
                    "User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user,
                    sharedWith);
                return new UnauthorizedResult();
            }
        }
        else if (sharedVideoInformation.SharingWithType == SharingWithType.Private)
        {
            // Private videos: no group/event access check needed
            // SharedWith should be null for private videos
            if (sharedWith != null)
            {
                ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "SharedWith must be null for Private sharing type.");
                return BadRequest(ModelState);
            }
        }
        else
        {
            return new BadRequestResult();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var sharedBlob = await videoService.GetSharingLink(
            user,
            sharedVideoInformation.NameOfVideo,
            sharedVideoInformation.FileName,
            sharedVideoInformation.SharingWithType,
            sharedVideoInformation.SharedWith,
            cancellationToken);

        return new UploadVideoInformationResponse()
        {
            Sas = sharedBlob.Sas.ToString(), VideoId = sharedBlob.VideoId, ExpireAt = sharedBlob.ExpireAt
        };
    }
}