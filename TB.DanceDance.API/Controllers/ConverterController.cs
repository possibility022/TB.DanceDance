﻿using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> GetVideosToConvert()
    {
        var video = await videoUploaderService.GetNextVideoToTransformAsync();

        if (video == null)
            return NotFound();

        var sas = videoUploaderService.GetVideoSas(video.BlobId);

        return Ok(new VideoToTransform()
        {
            Id = video.Id,
            Sas = sas.ToString(),
        });
    }

    [HttpPost]
    [Route(ApiEndpoints.Converter.UpdateInfo)]
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
}
