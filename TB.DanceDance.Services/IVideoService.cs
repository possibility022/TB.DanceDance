using MongoDB.Driver;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public interface IVideoService
    {
        Task<Stream> OpenStream(string blobName);
        Task<IEnumerable<VideoInformation>> GetVideos(FilterDefinition<VideoInformation>? filter = null);
    }
}