using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.Data.Blobs;

namespace TB.DanceDance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly IBlobDataService blobService;

        public StreamController(IBlobDataService blobService)
        {
            this.blobService = blobService;
        }


        [HttpGet]
        public string Get()
        {
            return "BLABLA";
        }

        [HttpGet]
        [Route("{blobId}")]
        public async Task<IActionResult> GetStream(string blobId)
        {
            if (!Request.Query.ContainsKey("userHash"))
                return new UnauthorizedResult();

            var hash = Request.Query["userHash"];
            if (!LoginCache.CheckIfLoggedIn(hash))
            {
                return new UnauthorizedResult();
            }


            var stream = await blobService.OpenStream(blobId);
            return File(stream, "video/mp4", enableRangeProcessing: true);
        }

    }
}
