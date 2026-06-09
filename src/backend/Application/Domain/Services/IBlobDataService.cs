using Domain.Models;

namespace Domain.Services;

public interface IBlobDataService
{
    Uri GetReadSas(string blobId);
    Uri GetReadSas(string blobId, DateTimeOffset expiresOn);
    Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken);
    Task Upload(string blobId, Stream stream);
    SharedBlob GetUploadSas(string? blobId = null);
    Task<bool> BlobExistsAsync(string blobId);
    Task<long> GetBlobSizeAsync(string blobId);

    /// <summary>
    /// Deletes the blob (including snapshots) if it exists. No-op when the blob is absent.
    /// </summary>
    Task DeleteAsync(string blobId, CancellationToken cancellationToken = default);
}