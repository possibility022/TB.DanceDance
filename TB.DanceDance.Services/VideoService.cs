using Microsoft.EntityFrameworkCore;
using System.Text;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;

public class VideoService : IVideoService
{
    private readonly DanceDbContext dbContext;
    private readonly IBlobDataService blobService;
    private readonly IVideoFileLoader videoFileLoader;
    private readonly IVideoUploaderService videoUploaderService;

    public VideoService(
        DanceDbContext dbContext,
        IBlobDataServiceFactory blobServiceFactory,
        IVideoFileLoader videoFileLoader,
        IVideoUploaderService videoUploaderService)
    {
        this.dbContext = dbContext;
        this.blobService = blobServiceFactory.GetBlobDataService(BlobContainer.Videos);
        this.videoFileLoader = videoFileLoader ?? throw new ArgumentNullException(nameof(videoFileLoader));
        this.videoUploaderService = videoUploaderService;
    }

    private IQueryable<VideoInfo> GetBaseVideosForUserQuery(string userId)
    {
        return from video in dbContext.Videos
               join sharedWith in dbContext.SharedWith on video.Id equals sharedWith.VideoId
               join events in dbContext.Events.DefaultIfEmpty() on sharedWith.EventId equals events.Id into eventsGroup
               from events in eventsGroup.DefaultIfEmpty()
               join groups in dbContext.Groups.DefaultIfEmpty() on sharedWith.GroupId equals groups.Id into groupsGroup
               from groups in groupsGroup.DefaultIfEmpty()
               join eventsAssignments in dbContext.AssingedToEvents.DefaultIfEmpty() on events.Id equals eventsAssignments.EventId into eventsAssignmentsGroup
               from eventsAssignments in eventsAssignmentsGroup.DefaultIfEmpty()
               join groupsAssignments in dbContext.AssingedToGroups.DefaultIfEmpty() on groups.Id equals groupsAssignments.GroupId into groupsAssignmentsGroup
               from groupsAssignments in groupsAssignmentsGroup.DefaultIfEmpty()
               where
               sharedWith.UserId == userId || eventsAssignments.UserId == userId || groupsAssignments.UserId == userId
               orderby video.RecordedDateTime descending
               select new VideoInfo
               {
                   Video = video,
                   SharedWithEvent = eventsAssignments != null,
                   SharedWithGroup = groupsAssignments != null,
               };
    }
    public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId)
    {
        var query = GetBaseVideosForUserQuery(userId)
            .Where(v => v.Video.BlobId == videoBlobId)
            .AnyAsync();

        var any = await query;

        return any;
    }

    public async Task<Video> UploadVideoAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            throw new IOException("File not found: " + filePath);

        (var info, var metada) = await videoFileLoader.CreateRecord(filePath);

        dbContext.Videos.Add(info);
        await dbContext.SaveChangesAsync();

        var videoMetadata = new VideoMetadata()
        {
            VideoId = info.Id,
            Metadata = Encoding.UTF8.GetBytes(metada)
        };

        dbContext.VideoMetadata.Add(videoMetadata);
        await dbContext.SaveChangesAsync();

        await blobService.Upload(info.BlobId, File.OpenRead(filePath));

        return info;
    }

    public Task<VideoInfo?> GetVideoByBlobAsync(string userId, string blobId)
    {
        return GetBaseVideosForUserQuery(userId)
            .Where(r => r.Video.BlobId == blobId)
            .FirstOrDefaultAsync();
    }

    public IQueryable<VideoInfo> GetVideos(string userId)
    {
        var query = GetBaseVideosForUserQuery(userId);
        return query;
    }

    public async Task<IQueryable<Video>> GetVideos()
    {
        var query = dbContext
            .Videos
            .Include(r => r.SharedWith)
            .OrderByDescending(r => r.RecordedDateTime);

        //todo add paging

        return query;
    }

    public Task<Stream> OpenStream(string blobName)
    {
        return blobService.OpenStream(blobName);
    }

    public Task<Event> GetEvent(Guid id)
    {
        return dbContext.Events.FirstAsync(r => r.Id == id);
    }

    public Task<Group> GetGroup(Guid id)
    {
        return dbContext.Groups.FirstAsync(group => group.Id == id);
    }

    public async Task RenameVideoAsync(Guid guid, string newName)
    {
        var video = await dbContext.Videos.FirstAsync(r => r.Id == guid);

        video.Name = newName;
        dbContext.SaveChanges();
    }

    public async Task<SharedBlob> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith)
    {
        var sharedBlob = videoUploaderService.GetSasUri();

        var video = new VideoToTranform()
        {
            FileName = fileName,
            AssignedToEvent = assignedToEvent,
            BlobId = sharedBlob.BlobClient.Name,
            Name = name,
            UploadedBy = userId,
            Duration = null,
            RecordedDateTime = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
            SharedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            SharedWithId = sharedWith
        };

        dbContext.VideosToTranform.Add(video);
        await dbContext.SaveChangesAsync();

        return sharedBlob;
    }
}