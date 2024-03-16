using Domain.Entities;

namespace Domain.Services;

public interface IVideoUploaderService
{
    SharedBlob GetSasUri();

    Task<Video?> GetNextVideoToTransformAsync();
    Task<bool> UpdateVideoInformations(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata);
    Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId);
    Uri GetVideoSas(string blobId);
    Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId);
}
