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
                           IUserService userService,
                           ILogger<VideoController> logger)
    {
        this.videoService = videoService;
        this.tokenValidator = tokenValidator;
        this.userService = userService;
        this.logger = logger;
    }

    private readonly IVideoService videoService;
    private readonly ITokenValidator tokenValidator;
    private readonly IUserService userService;
    private readonly ILogger<VideoController> logger;


    [Route(ApiEndpoints.Video.GetSingle)]
    [HttpGet]
    public async Task<IActionResult> GetInformationAsync(string guid)
    {
        string user = User.GetSubject();

        var info = await videoService.GetVideoByBlobAsync(user, guid);

        if (info == null)
            return NotFound();

        var results = ContractMappers.MapToVideoInformation(info);

        return new OkObjectResult(results);
    }

    [Route(ApiEndpoints.Video.GetStream)]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStreamAsync(string guid, [FromQuery] string token)
    {
        // todo create better authentication. Send send tokens in headers
        var validationRes = await tokenValidator.ValidateAccessTokenAsync(token);
        if (validationRes == null)
            // Idk when this can happen
            throw new Exception("Results of validation are null.");

        if (validationRes.IsError)
            return Unauthorized();

        var userSubjectId = validationRes.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (userSubjectId == null)
            return BadRequest();

        var hasAccess = await videoService.DoesUserHasAccessAsync(guid, userSubjectId);
        if (!hasAccess)
            return new UnauthorizedResult();

        var stream = await videoService.OpenStream(guid);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }

    [Route(ApiEndpoints.Video.Rename)]
    [HttpPost]
    public async Task<IActionResult> RenameVideo([FromRoute] Guid videoId, [FromBody] VideoRenameRequest input)
    {
        var res = await videoService.RenameVideoAsync(videoId, input.NewName);

        if (res == false)
            return BadRequest();

        return Ok();
    }

    [Route(ApiEndpoints.Video.GetUploadUrl)]
    [HttpPost]
    public async Task<ActionResult<UploadVideoInformation>> GetUploadInformation([FromBody] SharedVideoInformationRequest sharedVideoInformation)
    {
        string? user = null;
        var sharedWith = sharedVideoInformation?.SharedWith;

        if (sharedVideoInformation == null || sharedWith == null)
        {
            ModelState.AddModelError(nameof(sharedVideoInformation.SharedWith), "EntityId within SharedWith is empty.");
        }
        else
        {
            if (sharedVideoInformation.SharingWithType == SharingWithType.Group)
            {
                user = User.GetSubject();

                var canUploadToGroup = await userService.CanUserUploadToGroupAsync(user, sharedWith.Value);

                if (!canUploadToGroup)
                {
                    logger.LogWarning("User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user, sharedWith);
                    return new UnauthorizedResult();
                }
            }
            else if (sharedVideoInformation.SharingWithType == SharingWithType.Event)
            {
                user = User.GetSubject();

                var canUploadToEvent = await userService.CanUserUploadToEventAsync(user, sharedWith.Value);

                if (!canUploadToEvent)
                {
                    logger.LogWarning("User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user, sharedWith);
                    return new UnauthorizedResult();
                }
            }
            else
            {
                return new BadRequestResult();
            }
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // If this is a request where video id is provided, then api user want to continue upload
        // so, sas to the existing blob should be returned
        if (sharedVideoInformation!.VideoId is not null && sharedVideoInformation.VideoId != Guid.Empty)
        {
            var sharedLink = await videoService.GetSharingLink(sharedVideoInformation.VideoId.Value);

            if (sharedLink == null)
                return NotFound();

            return new UploadVideoInformation() { Sas = sharedLink.Sas.ToString(), VideoId = sharedLink.VideoId, ExpireAt = sharedLink.ExpireAt.UtcDateTime};
        }

        var sharedBlob = await videoService.GetSharingLink(
            user!,
            sharedVideoInformation!.NameOfVideo,
            sharedVideoInformation.FileName,
            sharedVideoInformation.SharingWithType == SharingWithType.Event,
            sharedVideoInformation!.SharedWith!.Value);

        return new UploadVideoInformation()
        {
            Sas = sharedBlob.Sas.ToString(),
            VideoId = sharedBlob.VideoId,
            ExpireAt = sharedBlob.ExpireAt.UtcDateTime
        };
    }


}