using IdentityServer4;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Identity.IdentityResources;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.ReadScope)]
public class VideoController : Controller
{

    public VideoController(IVideoService videoService, ITokenValidator tokenValidator, IUserService userService)
    {
        this.videoService = videoService;
        this.tokenValidator = tokenValidator;
        this.userService = userService;
    }

    private readonly IVideoService videoService;
    private readonly ITokenValidator tokenValidator;
    private readonly IUserService userService;

    [Route("api/video/getinformations")]
    [HttpGet]
    public async Task<IEnumerable<VideoInformation>> GetInformationsAsync()
    {
        string? user = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

        if (user == null)
        {
            return Array.Empty<VideoInformation>();
        }
        
        var userAssociations = await userService.GetUserVideosAssociationsIds(user);

        var filterBuilder = new FilterDefinitionBuilder<VideoInformation>();
        var f = filterBuilder
            .In(information => information.VideoOwner.OwnerId, userAssociations);

        return await videoService.GetVideos(f);
    }

    [Route("api/video/stream/{guid}")]
    [HttpGet]
    [Authorize(AuthenticationSchemes = IdentityServerConstants.DefaultCookieAuthenticationScheme)]
    public async Task<IActionResult> GetStreamAsync(string guid, [FromQuery] string token)
    {
        // todo create better authentication. Send send tokens in headers
        var validationRes = await tokenValidator.ValidateAccessTokenAsync(token ?? string.Empty);
        if (validationRes == null)
            // Idk when this can happen
            throw new Exception("Results of validation are null.");

        if (validationRes.IsError)
            return Unauthorized();

        var stream = await videoService.OpenStream(guid);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }
}
