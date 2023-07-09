using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Mappers;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;
using TB.DanceDance.Services.Models;

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

    [Route(ApiEndpoints.Video.GetAll)]
    [HttpGet]
    public async Task<IEnumerable<VideoInformation>> GetInformationAsync()
    {
        string user = User.GetSubject();

        var v = videoService.GetVideos(user).ToList();

        return videoService
            .GetVideos(user)
            .Select(r => ContractMappers.MapToVideoInformation(r));
    }

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
    public async Task<IActionResult> RenameVideo(string guid, [FromBody] VideoRenameModel input)
    {
        var res = Guid.TryParse(guid, out var parsedGuid);
        if (!res)
            return BadRequest();

        await videoService.RenameVideoAsync(parsedGuid, input.NewName);

        return Ok();
    }

    [Route(ApiEndpoints.Video.GetUploadUrl)]
    [HttpPost]
    public async Task<ActionResult<UploadVideoInformation>> GetUploadInformation([FromBody] SharedVideoInformation sharedVideoInformations)
    {
        string? user = null;
        var sharedWith = sharedVideoInformations?.SharedWith;

        if (sharedVideoInformations == null || sharedWith == null)
        {
            ModelState.AddModelError(nameof(sharedVideoInformations.SharedWith), "EntityId within SharedWith is empty.");
        }
        else
        {
            if (sharedVideoInformations.SharingWithType == SharingWithType.Group)
            {
                user = User.GetSubject();

                var canUploadToGroup = await userService.CanUserUploadToGroupAsync(user, sharedWith.Value);

                if (!canUploadToGroup)
                {
                    logger.LogWarning("User {0} was trying to add video where he is not assigned. Association EntityId: {1}.", user, sharedWith);
                    return new UnauthorizedResult();
                }
            }
            else if (sharedVideoInformations.SharingWithType == SharingWithType.Event)
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

        var sharedBlob = await videoService.GetSharingLink(
            user,
            sharedVideoInformations.NameOfVideo,
            sharedVideoInformations.SharingWithType == SharingWithType.Event,
            sharedVideoInformations.SharedWith.Value);

        return new UploadVideoInformation()
        {
            Sas = sharedBlob.Sas.ToString()
        };
    }


}