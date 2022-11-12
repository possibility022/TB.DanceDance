using Duende.IdentityServer;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Core;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers;

[Authorize(Config.ReadScope)]
public class VideoController : Controller
{

    public VideoController(IVideoService videoService, ITokenValidator tokenValidator)
    {
        this.videoService = videoService;
        this.tokenValidator = tokenValidator;
    }

    private readonly IVideoService videoService;
    private readonly ITokenValidator tokenValidator;

    [Route("api/video/getinformations")]
    [HttpGet]
    public async Task<IEnumerable<VideoInformation>> GetInformationsAsync()
    {
        return await videoService.GetVideos();
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
