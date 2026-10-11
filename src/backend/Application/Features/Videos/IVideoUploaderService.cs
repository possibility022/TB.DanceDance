using Domain.Entities;
using Domain.Models;
using TB.DanceDance.API.Contracts.Features.Videos.Converter;

namespace Application.Features.Videos;

public interface IVideoUploaderService
{
    SharedBlob GetUploadSasUri();
    SharedBlob GetUploadSasUri(string blobId);

    Task<Video?> GetNextVideoToTransformAsync(CancellationToken cancellationToken);
    Task<bool> UpdateVideoInformation(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata, CancellationToken cancellationToken);
    Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, CancellationToken cancellationToken);
    Uri GetVideoSas(string blobId);
    Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId, CancellationToken cancellationToken);

    Task<(Guid Id, string BlobId, string FileName, Uri Sas)?> GetNextVideoForThumbnailAsync(CancellationToken cancellationToken);
    Task<List<VideoMissingThumbnailModel>> GetVideosMissingThumbnailsAsync(int limit, CancellationToken cancellationToken);
    Task<SharedBlob?> GetSasForThumbnailUploadAsync(Guid videoId, CancellationToken cancellationToken);
    Task<bool> PublishThumbnailAsync(Guid videoId, CancellationToken cancellationToken);
}
