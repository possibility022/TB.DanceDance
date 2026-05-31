using Infrastructure.Identity.IdentityResources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TB.DanceDance.API.Contracts.Features.Conversion;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.API.Features.Conversion;

[Authorize(DanceDanceResources.WestCoastSwing.Scopes.WriteConvert)]
public class ConverterController : Controller
{
    private readonly IMediator mediator;

    public ConverterController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]
    [Route(ConversionRoutes.Videos)]
    public async Task<IActionResult> GetVideosToConvert(CancellationToken cancellationToken)
    {
        var video = await mediator.SendAsync(new GetNextVideoToConvertQuery(), cancellationToken);

        if (video == null)
            return NotFound();

        return Ok(new VideoToTransformResponse()
        {
            Id = video.Id,
            FileName = video.FileName,
            Sas = video.Sas.ToString(),
        });
    }

    [HttpPost]
    [Route(ConversionRoutes.Videos)]
    public async Task<IActionResult> UpdateVideoInfo([FromBody] UpdateVideoInfoRequest publishVideo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        var res = await mediator.SendAsync(new UpdateVideoInformationCommand()
        {
            VideoId = publishVideo.VideoId,
            Duration = publishVideo.Duration,
            Recorded = publishVideo.RecordedDateTime,
            Metadata = publishVideo.Metadata,
        }, cancellationToken);

        if (!res)
            return NotFound();

        return Ok();
    }

    [HttpGet]
    [Route(ConversionRoutes.GetPublishSas)]
    public async Task<IActionResult> GetPublishSas([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        var shared = await mediator.SendAsync(new GetPublishSasQuery(videoId), cancellationToken);

        if (shared == null)
            return NotFound();

        return Ok(new GetPublishSasResponse()
        {
            Sas = shared.Sas.ToString()
        });
    }

    [HttpPost]
    [Route(ConversionRoutes.Upload)]
    public async Task<IActionResult> PublishConvertedVideo([FromRoute] Guid videoId, CancellationToken cancellationToken)
    {
        var newId = await mediator.SendAsync(new UploadConvertedVideoCommand(videoId), cancellationToken);
        if (newId == null)
            return BadRequest();

        return Ok(new UploadConvertedVideoResponse() { VideoId = newId.Value });
    }
}
