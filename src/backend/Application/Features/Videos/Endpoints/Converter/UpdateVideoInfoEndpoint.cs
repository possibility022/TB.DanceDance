using FastEndpoints;
using FluentValidation;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;
using Void = FastEndpoints.Void;

namespace Application.Features.Videos.Endpoints.Converter;

public class UpdateVideoInfoValidator : Validator<UpdateVideoInfoRequest>
{
    public UpdateVideoInfoValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty().WithMessage("VideoId is required.");

        RuleFor(x => x.RecordedDateTime)
            .NotEmpty().WithMessage("RecordedDateTime is required.");

        RuleFor(x => x.Duration)
            .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be greater than zero.");
    }
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
        Post(ApiRoutes.Converter.Videos);
        Policies(ApiScopes.Convert);
    }
    
    public override async Task<Void> HandleAsync(UpdateVideoInfoRequest req, CancellationToken ct)
    {
        // Request is validated by UpdateVideoInfoValidator before this runs.
        var res = await videoUploaderService.UpdateVideoInformation(
            req.VideoId,
            req.Duration,
            req.RecordedDateTime,
            req.Metadata,
            ct
        );

        if (!res)
            return await Send.NotFoundAsync(ct);


        return await Send.NoContentAsync(ct);
    }
}