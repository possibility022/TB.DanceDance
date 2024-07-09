using Domain.Entities;

namespace Domain.Services;

public interface IVideoUploaderService
{
    SharedBlob GetSasUri();

    Task<Video?> GetNextVideoToTransformAsync(CancellationToken token);
    Task<bool> UpdateVideoInformations(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata, CancellationToken token);
    Task<Guid?> PublishConvertedVideo(Guid videoToConvertId, CancellationToken token);
    Uri GetVideoSas(string blobId);
    Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId, CancellationToken token);
}
