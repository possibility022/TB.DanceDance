using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.PostgreSQL.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;

public interface IVideoService
{
    Task<Stream> OpenStream(string blobName);
    IQueryable<VideoInfo> GetVideos(string userId);
    Task<VideoInfo?> GetVideoByBlobAsync(string userId, string blobId);
    Task<IQueryable<Video>> GetVideos();
    Task<Video> UploadVideoAsync(string filePath, CancellationToken cancellationToken);

    Task<Event> GetEvent(Guid id);
    Task<Group> GetGroup(Guid id);

    Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId);
    Task<bool> RenameVideoAsync(Guid guid, string newName);

    Task<SharedBlob> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith);
}