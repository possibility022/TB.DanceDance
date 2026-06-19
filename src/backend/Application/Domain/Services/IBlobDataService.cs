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
    /// Returns the blob's stored Content-Type, or null when it has none / is the Azure
    /// default ("application/octet-stream") — callers should fall back to a sensible
    /// default in that case (e.g. for blobs uploaded before Content-Type was set on upload).
    /// </summary>
    Task<string?> GetContentTypeAsync(string blobId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the blob (including snapshots) if it exists. No-op when the blob is absent.
    /// </summary>
    Task DeleteAsync(string blobId, CancellationToken cancellationToken = default);
}