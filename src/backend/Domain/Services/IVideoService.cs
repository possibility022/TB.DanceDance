using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken);
    Task<Video?> GetVideoByBlobAsync(string userId, string blobId, CancellationToken cancellationToken);
    Task<bool> RenameVideoAsync(Guid guid, string newName, CancellationToken cancellationToken);
    Task<UploadContext> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent,
        Guid sharedWith, CancellationToken cancellationToken);

    Task<UploadContext?> GetSharingLink(Guid videoId,CancellationToken cancellationToken);
}