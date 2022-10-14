using IdentityServer4;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.Db;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.API.Controllers;

[Authorize(Config.ReadScope)]
public class VideoController : Controller
{

    public VideoController(ApplicationDbContext dbContext, IBlobDataService videoBlobService, ITokenValidator tokenValidator)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.videoBlobService = videoBlobService ?? throw new ArgumentNullException(nameof(videoBlobService));
        this.tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
    }

    public ApplicationDbContext dbContext;
    private readonly IBlobDataService videoBlobService;
    private readonly ITokenValidator tokenValidator;

    [Route("api/video/getinformations")]
    [HttpGet]
    public IEnumerable<VideoInformation> GetInformations()
    {
        return dbContext.VideosInformation;
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

        var stream = await videoBlobService.OpenStream(guid);
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }
}
