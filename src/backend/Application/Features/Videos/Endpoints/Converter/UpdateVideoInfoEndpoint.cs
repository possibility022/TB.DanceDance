using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class UpdateVideoInfoEndpoint : Endpoint<UpdateVideoInfoRequest, EmptyResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public UpdateVideoInfoEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Post(ApiRoutes.Converter.Videos);
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