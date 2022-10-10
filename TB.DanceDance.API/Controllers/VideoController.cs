using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Data.Db;
using TB.DanceDance.Data.Models;

namespace TB.DanceDance.API.Controllers;

[Authorize(Config.ReadScope)]
public class VideoController : Controller
{

    public VideoController(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public ApplicationDbContext dbContext;

    [Route("api/video/getinformations")]
    [HttpGet]
    public IEnumerable<VideoInformation> GetInformations()
    {
        return dbContext.VideosInformation;
    }
}
