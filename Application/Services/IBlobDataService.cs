using Domain.Entities;

namespace Application.Services;

public interface IBlobDataService
{
    Uri GetSas(string blobId);
    Task<Stream> OpenStream(string blobName, CancellationToken token);
    Task Upload(string blobId, Stream stream, CancellationToken token);
    SharedBlob CreateUploadSas(string blobId = null);
    Task<bool> BlobExistsAsync(string blobId, CancellationToken token);
}