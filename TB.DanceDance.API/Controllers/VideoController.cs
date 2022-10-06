using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Models;
using static IdentityServer4.IdentityServerConstants;

namespace TB.DanceDance.API.Controllers;

[Authorize(LocalApi.PolicyName)]
public class VideoController : Controller
{
    [Route("api/video/getinformations")]
    [HttpGet]
    public IEnumerable<VideoInfo> GetInformations()
    {
        return new[]
        {
            new VideoInfo
            {
                VideoId = "Id",
                VideoName = "Xyz"
            }
        };
    }
}
