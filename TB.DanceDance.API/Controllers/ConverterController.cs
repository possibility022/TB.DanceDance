using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;
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
    [Route(ApiEndpoints.Converter.Videos)]
    public async Task<IActionResult> GetVideosToConvert()
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync();

        if (video == null)
            return NotFound();

        var sas = videoUploaderService.GetVideoSas(video.BlobId);

        return Ok(new VideoToTransformResponse()
        {
            Id = video.Id,
            FileName = video.FileName,
            Sas = sas.ToString(),
        });
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.Videos)]
    public async Task<IActionResult> UpdateVideoInfo([FromBody] UpdateVideoInfoRequest publishVideo)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var res = await videoUploaderService.UpdateVideoToTransformInformationAsync(
            publishVideo.VideoId,
            publishVideo.Duration,
            publishVideo.RecordedDateTime,
            publishVideo.Metadata
            );

        if (!res)
            return NotFound();


        return Ok();
    }

    [HttpGet]
    [Route(ApiEndpoints.Converter.GetPublishSas)]
    public async Task<IActionResult> GetPublishSas([FromRoute] Guid videoId)
    {
        var shared = await videoUploaderService.GetSasForConvertedVideoAsync(videoId);

        return Ok(new GetPublishSasResponse()
        {
            Sas = shared.Sas.ToString()
        });
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.Upload)]
    public async Task<IActionResult> PublishConvertedVideo([FromRoute]Guid videoId)
    {
        var newId = await videoUploaderService.UploadConvertedVideoAsync(videoId);
        if (newId == null)
            return BadRequest();

        return Ok(new UploadConvertedVideoResponse() { VideoId = newId.Value});
    }
}
