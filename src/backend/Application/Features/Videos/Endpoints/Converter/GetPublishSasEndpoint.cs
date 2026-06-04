using FastEndpoints;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;

namespace Application.Features.Videos.Endpoints.Converter;

public class GetPublishSasEndpoint : Endpoint<GetPublishSasRequest, GetPublishSasResponse>
{
    private readonly IVideoUploaderService videoUploaderService;

    public GetPublishSasEndpoint(IVideoUploaderService videoUploaderService)
    {
        this.videoUploaderService = videoUploaderService;
    }

    public override void Configure()
    {
        Get(ApiRoutes.Converter.GetPublishSas);
        Policies(ApiScopes.Convert);
    }

    public override async Task HandleAsync(GetPublishSasRequest req, CancellationToken ct)
    {
        var shared = await videoUploaderService.GetSasForConvertedVideoAsync(req.VideoId, ct);
        
        if (shared == null)
            throw new InvalidOperationException("Failed to retrieve SAS for converted video");

        await Send.OkAsync(new GetPublishSasResponse()
        {
            Sas = shared.Sas.ToString()
        }, ct);
    }
}