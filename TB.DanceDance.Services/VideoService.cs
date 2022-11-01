using MongoDB.Driver;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public class VideoService : IVideoService
    {
        IMongoCollection<VideoInformation> videoCollection;
        private readonly IBlobDataService blobService;
        private readonly IVideoFileLoader videoFileLoader;

        public VideoService(IMongoCollection<VideoInformation> videoCollection, IBlobDataService blobService, IVideoFileLoader videoFileLoader)
        {
            this.videoCollection = videoCollection ?? throw new ArgumentNullException(nameof(videoCollection));
            this.blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
            this.videoFileLoader = videoFileLoader ?? throw new ArgumentNullException(nameof(videoFileLoader));
        }

        public async Task<VideoInformation> UploadVideoAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
                throw new IOException("File not found: " + filePath);

            var info = await videoFileLoader.CreateRecord(filePath);

            await videoCollection.InsertOneAsync(info, new InsertOneOptions()
            {

            },
            cancellationToken);

            await blobService.Upload(info.BlobId, File.OpenRead(filePath));

            return info;
        }

        public async Task<IEnumerable<VideoInformation>> GetVideos(FilterDefinition<VideoInformation>? filter = null)
        {
            if (filter == null)
                filter = FilterDefinition<VideoInformation>.Empty;

            var find = videoCollection.Find(filter);

            var list = await find.ToListAsync();
            return list;
        }

        public Task<Stream> OpenStream(string blobName)
        {
            return blobService.OpenStream(blobName);
        }

    }
}