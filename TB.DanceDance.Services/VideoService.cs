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
        private readonly IMongoCollection<SharedVideo> sharedVideos;
        private readonly IBlobDataService blobService;
        private readonly IVideoFileLoader videoFileLoader;

        public VideoService(IMongoCollection<VideoInformation> videoCollection,
            IMongoCollection<Event> events,
            IMongoCollection<Group> groups,
            IMongoCollection<SharedVideo> sharedVideos,
            IBlobDataServiceFactory blobServiceFactory,
            IVideoFileLoader videoFileLoader)
        {
            this.videoCollection = videoCollection ?? throw new ArgumentNullException(nameof(videoCollection));
            this.events = events;
            this.groups = groups;
            this.sharedVideos = sharedVideos;
            this.blobService = blobServiceFactory.GetBlobDataService(BlobContainer.Videos);
            this.videoFileLoader = videoFileLoader ?? throw new ArgumentNullException(nameof(videoFileLoader));
        }

        public async Task<SharingScope> GetSharedWith(string videoBlobId)
        {
            var res = await videoCollection.Find(r => r.BlobId == videoBlobId)
                .SingleAsync();

            return res.SharedWith;
        }

        public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId)
        {
            var sharingScope = await GetSharedWith(videoBlobId);
            var allowedUsers = await GetOwnerAssociatedUsers(sharingScope);
            return allowedUsers.Contains(userId);
        }

        private async Task<ICollection<string>> GetOwnerAssociatedUsers(SharingScope owner)
        {
            if (owner.Assignment == AssignmentType.Event)
            {
                var ownerEntity = await events.Find(r => r.Id == owner.EntityId)
                    .SingleAsync();

                return ownerEntity.Attenders;
            }
            else if (owner.Assignment == AssignmentType.Group)
            {
                var ownerEntity = await groups.Find(r => r.Id == owner.EntityId)
                    .SingleAsync();

                return ownerEntity.People;
            }
            else if (owner.Assignment == AssignmentType.Person)
            {
                return new[] { owner.EntityId };
            }

            throw new ArgumentOutOfRangeException(nameof(owner.Assignment), "Could not resolve associations for owner type: " + owner.Assignment.ToString());
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

        public Task<Event> GetEvent(string id)
        {
            return events.Find(@event => @event.Id == id)
                .SingleAsync();
        }

        public Task<Group> GetGroup(string id)
        {
            return groups.Find(group => group.Id == id)
                .SingleAsync();
        }

        public async Task SaveSharedVideoInformations(SharedVideo sharedVideo)
        {
            await sharedVideos.InsertOneAsync(sharedVideo);
        }
    }
}