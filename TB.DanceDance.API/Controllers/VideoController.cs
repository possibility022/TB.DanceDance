using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Models;

namespace TB.DanceDance.API.Controllers;

[Authorize(Config.ReadScope)]
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
