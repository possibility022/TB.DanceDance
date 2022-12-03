using MongoDB.Driver;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb.Models;

namespace TB.DanceDance.Services
{
    public class VideoService : IVideoService
    {
        readonly IMongoCollection<VideoInformation> videoCollection;
        private readonly IMongoCollection<Event> events;
        private readonly IMongoCollection<Group> groups;
        private readonly IBlobDataService blobService;
        private readonly IVideoFileLoader videoFileLoader;

        public VideoService(IMongoCollection<VideoInformation> videoCollection,
            IMongoCollection<Event> events,
            IMongoCollection<Group> groups,
            IBlobDataService blobService,
            IVideoFileLoader videoFileLoader)
        {
            this.videoCollection = videoCollection ?? throw new ArgumentNullException(nameof(videoCollection));
            this.events = events;
            this.groups = groups;
            this.blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
            this.videoFileLoader = videoFileLoader ?? throw new ArgumentNullException(nameof(videoFileLoader));
        }

        public async Task<VideoOwner> GetVideoOwner(string videoBlobId)
        {
            var res = await videoCollection.Find(r => r.BlobId == videoBlobId)
                .SingleAsync();

            return res.VideoOwner;
        }

        public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId)
        {
            var owner = await GetVideoOwner(videoBlobId);
            var allowedUsers = await GetOwnerAssociatedUsers(owner);
            return allowedUsers.Contains(userId);
        }

        private async Task<ICollection<string>> GetOwnerAssociatedUsers(VideoOwner owner)
        {
            if (owner.OwnerType == OwnerType.Event)
            {
                var ownerEntity = await events.Find(r => r.Id == owner.OwnerId)
                    .SingleAsync();

                return ownerEntity.Attenders;
            } else if (owner.OwnerType == OwnerType.Group)
            {
                var ownerEntity = await groups.Find(r => r.Id == owner.OwnerId)
                    .SingleAsync();

                return ownerEntity.People;
            } else if (owner.OwnerType == OwnerType.Person)
            {
                return new[] { owner.OwnerId };
            }

            throw new ArgumentOutOfRangeException(nameof(owner.OwnerType), "Could not resolve associations for owner type: " + owner.OwnerType.ToString());
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