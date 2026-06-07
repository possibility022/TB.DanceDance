using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;

namespace Application.Features.Videos.Endpoints.Converter;

public class GetThumbnailSasEndpoint : EndpointWithoutRequest<GetThumbnailSasResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public GetThumbnailSasEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Converter.GetThumbnailSas);
        Policies(ApiScopes.Convert);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var videoId = Route<Guid>("videoId");
        var shared = await videoUploaderService.GetSasForThumbnailUploadAsync(videoId, ct);

        if (shared == null)
            throw new InvalidOperationException("Failed to retrieve SAS for thumbnail upload");

        await Send.OkAsync(new GetThumbnailSasResponse { Sas = shared.Sas.ToString() }, ct);
    }
}
