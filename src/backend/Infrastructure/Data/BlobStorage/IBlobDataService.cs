using Domain.Entities;

namespace Application.Services;

public interface IBlobDataService
{
    Uri GetReadSas(string blobId);
    Task<Stream> OpenStream(string blobName);
    Task Upload(string blobId, Stream stream);
    SharedBlob GetUploadSas(string? blobId = null);
    Task<bool> BlobExistsAsync(string blobId);
}