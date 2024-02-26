using Domain.Entities;

namespace Application.Services;

public interface IBlobDataService
{
    Uri GetSas(string blobId);
    Task<Stream> OpenStream(string blobName);
    Task Upload(string blobId, Stream stream);
    SharedBlob CreateUploadSas(string blobId = null);
    Task<bool> BlobExistsAsync(string blobId);
}