using Domain.Entities;

namespace Domain.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName, CancellationToken token);
    Task<Video?> GetVideoByBlobAsync(string userId, string blobId, CancellationToken token);
    Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId, CancellationToken token);
    Task<bool> RenameVideoAsync(Guid guid, string newName, CancellationToken token);
    Task<SharedBlob> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith, CancellationToken token);
}