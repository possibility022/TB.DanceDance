using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IVideoUploaderService
{
    SharedBlob GetUploadSasUri();
    SharedBlob GetUploadSasUri(string blobId);

    Task<Video?> GetNextVideoToTransformAsync(CancellationToken cancellationToken);
    Task<bool> UpdateVideoInformation(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata, CancellationToken cancellationToken);
    Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, CancellationToken cancellationToken);
    Uri GetVideoSas(string blobId);
    Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId, CancellationToken cancellationToken);
}
