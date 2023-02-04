using IdentityServer4;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TB.DanceDance.API.Extensions;
using TB.DanceDance.API.Models;
using TB.DanceDance.Data.MongoDb.Models;
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
                           IVideoUploaderService videoUploaderService,
                           ILogger<VideoController> logger)
    {
        this.videoService = videoService;
        this.tokenValidator = tokenValidator;
        this.userService = userService;
        this.videoUploaderService = videoUploaderService;
        this.logger = logger;
    }

    private readonly IVideoService videoService;
    private readonly ITokenValidator tokenValidator;
    private readonly IUserService userService;
    private readonly IVideoUploaderService videoUploaderService;
    private readonly ILogger<VideoController> logger;

    [Route("api/video/getinformations")]
    [HttpGet]
    public async Task<IEnumerable<VideoInformation>> GetInformationsAsync()
    {
        string user = User.GetSubject();

        var userAssociations = await userService.GetUserVideosAssociationsIds(user);

        var filterBuilder = new FilterDefinitionBuilder<VideoInformation>();
        var f = filterBuilder
            .In(information => information.SharedWith.EntityId, userAssociations);

        return await videoService.GetVideos(f);
    }

    [Route("api/video/stream/{guid}")]
    [HttpGet]
    [Authorize(AuthenticationSchemes = IdentityServerConstants.DefaultCookieAuthenticationScheme)]
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

    [Route("/api/video/getUploadUrl")]
    public async Task<ActionResult<UploadVideoInformation>> GetUploadInformation(SharedVideoInformation sharedVideoInformations)
    {
        string? user = null;
        var sharedWith = sharedVideoInformations?.SharedWith;

        if (string.IsNullOrEmpty(sharedWith?.EntityId))
        {
            ModelState.AddModelError(nameof(sharedVideoInformations.SharedWith), "EntityId within SharedWith is empty.");
        }
        else
        {
            if (sharedWith.Assignment == AssignmentType.Event || sharedWith.Assignment == AssignmentType.Group)
            {
                user = User.GetSubject();
                var isAssigned = await userService.UserIsAssociatedWith(user, sharedWith.EntityId);
                if (!isAssigned)
                {
                    logger.LogWarning("User {0} was trying to add video where he is not assigned. Association EntityId: {1}. Assigment type: {2}", user, sharedWith.EntityId, sharedWith.Assignment);
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

        var sharedBlob = videoUploaderService.GetSasUri();

        await videoService.SaveSharedVideoInformations(new SharedVideo()
        {
            Shared = DateTime.UtcNow,
            VideoInformation = new VideoInformation()
            {
                SharedWith = sharedVideoInformations.SharedWith,
                BlobId = sharedBlob.BlobClient.Name,
                Name = sharedVideoInformations.NameOfVideo,
                SharedDateTimeUtc = DateTime.UtcNow,
                UploadedBy = new SharingScope() //todo, use factory to create those objects
                {
                    Assignment = AssignmentType.Person,
                    EntityId = user ?? throw new ArgumentNullException("User id not specified.") //todo throw specific exception
                },
            }
        });

        return new UploadVideoInformation()
        {
            Sas = sharedBlob.Sas.ToString()
        };
    }


}