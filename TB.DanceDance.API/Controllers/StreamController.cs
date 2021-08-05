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
        public async Task<Stream> GetStream(string blobId)
        {

            var rangeHeader = Request.GetTypedHeaders().Range;
            var from = rangeHeader?.Ranges?.FirstOrDefault()?.From;

            if (@from is > 0)
            {
                // Return part of the video
                //HttpResponseMessage partialResponse = new ;
                //partialResponse.Content = new ByteRangeStreamContent(stream, Request.Headers.Range, mediaType);
                //return partialResponse;
                
                    var stream = await blobService.OpenStream(blobId);
                    stream.Seek(from.Value, SeekOrigin.Begin);

                    return stream;
            }
            else
            {
                // Return complete video



                var stream = await blobService.OpenStream(blobId);

                //var response = new HttpResponseMessage(HttpStatusCode.OK);
                //response.Content = new StreamContent(stream);

                return stream;

                //HttpResponseMessage fullResponse = Request.CreateResponse(HttpStatusCode.OK);
                //fullResponse.Content = new StreamContent(stream);
                //fullResponse.Content.Headers.ContentType = mediaType;
                //return fullResponse;
            }
        }

    }
}
