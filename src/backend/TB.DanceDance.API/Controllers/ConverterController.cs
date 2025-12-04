using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;

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
    public async Task<IActionResult> GetVideosToConvert(CancellationToken cancellationToken)
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync(cancellationToken);

        if (video == null)
            return NotFound();

        var sas = videoUploaderService.GetVideoSas(video.SourceBlobId);

        return Ok(new VideoToTransformResponse()
        {
            Id = video.Id,
            FileName = video.FileName,
            Sas = sas.ToString(),
        });
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.Videos)]
    public async Task<IActionResult> UpdateVideoInfo([FromBody] UpdateVideoInfoRequest publishVideo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var res = await videoUploaderService.UpdateVideoInformation(
            publishVideo.VideoId,
            publishVideo.Duration,
            publishVideo.RecordedDateTime,
            publishVideo.Metadata,
            cancellationToken
            );

        if (!res)
            return NotFound();


        return Ok();
    }

    [HttpGet]
    [Route(ApiEndpoints.Converter.GetPublishSas)]
    public async Task<IActionResult> GetPublishSas([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        var shared = await videoUploaderService.GetSasForConvertedVideoAsync(videoId, cancellationToken);

        return Ok(new GetPublishSasResponse()
        {
            Sas = shared.Sas.ToString()
        });
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.Upload)]
    public async Task<IActionResult> PublishConvertedVideo([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        var newId = await videoUploaderService.UploadConvertedVideoAsync(videoId, cancellationToken);
        if (newId == null)
            return BadRequest();

        return Ok(new UploadConvertedVideoResponse() { VideoId = newId.Value });
    }
}
