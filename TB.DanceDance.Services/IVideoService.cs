using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public interface IVideoService
    {
        Task<Stream> OpenStream(string blobName);
        Task<IEnumerable<VideoInformation>> GetVideos(FilterDefinition<VideoInformation>? filter = null);
        Task<SharingScope> GetSharedWith(string videoBlobId);
        Task<VideoInformation> UploadVideoAsync(string filePath, CancellationToken cancellationToken);

        Task<Event> GetEvent(string id);
        Task<Group> GetGroup(string id);

        Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId);

        Task SaveSharedVideoInformations(SharedVideo sharedVideo);
    }
}