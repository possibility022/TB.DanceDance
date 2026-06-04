using FastEndpoints;
using System.ComponentModel.DataAnnotations;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public record  UpdateVideoInfoRequest
{
    [Required]
    public Guid VideoId { get; set; }

    [Required]
    public DateTime RecordedDateTime { get; set; }

    [Required]
    public TimeSpan Duration { get; set; }

    [Required]
    public byte[]? Metadata { get; set; }
}

public class UpdateVideoInfoEndpoint : Endpoint<UpdateVideoInfoRequest, EmptyResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public UpdateVideoInfoEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Converter.Upload);
        Policies(ApiScopes.Convert);
    }
    
    public override async Task<Void> HandleAsync(UpdateVideoInfoRequest req, CancellationToken ct)
    {
        // todo, validation
        // if (!ModelState.IsValid)
        //     return BadRequest();

        var res = await videoUploaderService.UpdateVideoInformation(
            req.VideoId,
            req.Duration,
            req.RecordedDateTime,
            req.Metadata,
            ct
        );

        if (!res)
            return await Send.NotFoundAsync(ct);


        return await Send.OkAsync(ct);
    }
}