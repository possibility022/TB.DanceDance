using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts;
using TB.DanceDance.Services;

namespace TB.DanceDance.API.Controllers;

public class ConverterController : Controller
{
    private readonly IVideoUploaderService videoUploaderService;

    public ConverterController(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    [HttpGet]
    [Route(ApiEndpoints.Converter.GetVideo)]
    public async Task<VideoToTransform> GetVideosToConvert()
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync();
        var sas = videoUploaderService.GetVideoSas(video.BlobId);

        return new VideoToTransform()
        {
            Id = video.Id,
            Sas = sas.ToString(),
        };
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.UpdateInfo)]
    public async Task<IActionResult> UpdateVideoInfo([FromBody] UpdateVideoInfoRequest publishVideo)
    {
        await videoUploaderService.UpdateVideoToTransformInformationAsync(
            publishVideo.VideoId,
            publishVideo.Duration,
            publishVideo.RecordedDateTime,
            publishVideo.Metadata
            );


        return Ok();
    }
}
