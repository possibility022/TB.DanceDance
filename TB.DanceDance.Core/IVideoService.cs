using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName);
    Task<Video?> GetVideoByBlobAsync(string userId, string blobId);
    Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId);
    Task<bool> RenameVideoAsync(Guid guid, string newName);
    Task<SharedBlob> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith);
}