using Domain.Entities;
using Domain.Models;

namespace Domain.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName);
    Task<Video?> GetVideoByBlobAsync(string userId, string blobId);
    Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId);
    Task<bool> DoesUserHasAccessAsync(Guid videoId, string userId);
    Task<bool> RenameVideoAsync(Guid guid, string newName);
    Task<UploadContext> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent,
        Guid sharedWith);

    Task<UploadContext?> GetSharingLink(Guid videoId);
}