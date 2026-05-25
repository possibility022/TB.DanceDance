using TB.DanceDance.Utilities.Infrastructure.Models;

namespace TB.DanceDance.Utilities.Infrastructure;

public interface IBlobDataService
{
    Uri GetReadSas(string blobId);
    Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken);
    Task Upload(string blobId, Stream stream);
    SharedBlob GetUploadSas(string? blobId = null);
    Task<bool> BlobExistsAsync(string blobId);
    Task<long> GetBlobSizeAsync(string blobId);
}