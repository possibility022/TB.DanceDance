using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public interface IVideoService
    {
        Task<Stream> OpenStream(string blobName);
        Task<IEnumerable<VideoInformation>> GetVideos(FilterDefinition<VideoInformation>? filter = null);
        Task<VideoOwner> GetVideoOwner(string videoBlobId);
        Task<VideoInformation> UploadVideoAsync(string filePath, CancellationToken cancellationToken);

        Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId);
    }
}