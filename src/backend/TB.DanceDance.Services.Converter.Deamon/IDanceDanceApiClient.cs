using TB.DanceDance.API.Contracts.Requests;
using TB.DanceDance.API.Contracts.Responses;

namespace TB.DanceDance.Services.Converter.Deamon;

internal interface IDanceDanceApiClient
{
    Task<VideoToTransformResponse?> GetNextVideoToConvertAsync(CancellationToken token);
    Task GetVideoToConvertAsync(Stream target, Uri videoUrl, CancellationToken token);
    Task UploadVideoToTransformInformation(UpdateVideoInfoRequest updateVideoInfoRequest, CancellationToken token);
    Task UploadContent(Guid videoId, Stream content, CancellationToken token);
    Task PublishTransformedVideo(Guid videoId, CancellationToken token);
}
