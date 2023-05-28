using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.MongoDb.Models;
using TB.DanceDance.Data.PostgreSQL;

namespace TB.DanceDance.Services
{
    public class VideoService : IVideoService
    {
        private readonly DanceDbContext dbContext;
        readonly IMongoCollection<VideoInformation> videoCollection;
        private readonly IMongoCollection<Event> events;
        private readonly IMongoCollection<Group> groups;
        private readonly IMongoCollection<SharedVideo> sharedVideos;
        private readonly IBlobDataService blobService;
        private readonly IVideoFileLoader videoFileLoader;

        public VideoService(
            DanceDbContext dbContext,
            IMongoCollection<VideoInformation> videoCollection,
            IMongoCollection<Event> events,
            IMongoCollection<Group> groups,
            IMongoCollection<SharedVideo> sharedVideos,
            IBlobDataServiceFactory blobServiceFactory,
            IVideoFileLoader videoFileLoader)
        {
            this.dbContext = dbContext;
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

                return ownerEntity.Attenders
                    .Select(r => r.ToString()) //todo use guids
                    .ToList();
            }
            else if (owner.Assignment == AssignmentType.Group)
            {
                var ownerEntity = await groups.Find(r => r.Id == owner.EntityId)
                    .SingleAsync();

                return ownerEntity.People
                    .Select(r => r.ToString()) //todo use guids
                    .ToList();
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

        public async Task<IEnumerable<VideoInformation>> GetVideos(FilterDefinition<VideoInformation>? filter = null, int? limit = null, bool includeMetadataAsJson = false)
        {
            var query = dbContext
                .Videos
                .Include(r => r.SharedWith)
                .OrderByDescending(r => r.RecordedDateTime)
                .Select(r => new VideoInformation()
                {
                    BlobId = r.BlobId,
                    Duration = r.Duration,
                    Id = r.Id.ToString(),
                    RecordedTimeUtc = r.RecordedDateTime,
                    Name = r.Name,
                    SharedDateTimeUtc = r.SharedDateTime,
                    UploadedBy = new SharingScope
                    {
                        Assignment = AssignmentType.Person,
                        EntityId = r.UploadedBy
                    }
                });

            // todo support MetadataAsJson = r.MetadataAsJson,

            if (limit.HasValue)
                query = query.Take(limit.Value);

            var results = await query.ToListAsync();
            return results;
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

        public async Task RenameVideoAsync(string guid, string newName)
        {
            var updateBuilder = new UpdateDefinitionBuilder<VideoInformation>();
            updateBuilder.Set(r => r.Name, newName);
            var update = updateBuilder.Combine();

            await videoCollection.UpdateOneAsync(f => f.Id == guid, update);
        }
    }
}