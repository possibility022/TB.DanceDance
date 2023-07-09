using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Data.Blobs;
using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Data.PostgreSQL.Models;

namespace TB.DanceDance.Services;

public interface IVideoUploaderService
{
    SharedBlob GetSasUri();

    Task<VideoToTranform?> GetNextVideoToTransformAsync();
    Task<bool> UpdateVideoToTransformInformationAsync(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata);
    Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, Stream data);
    Uri GetVideoSas(string blobId);
}

public class VideoUploaderService : IVideoUploaderService
{
    private readonly IBlobDataService videosToConvertBlobs;
    private readonly IBlobDataService publishedVideosBlobs;
    private readonly DanceDbContext danceDbContext;

    public VideoUploaderService(IBlobDataServiceFactory factory, DanceDbContext danceDbContext)
    {
        videosToConvertBlobs = factory.GetBlobDataService(BlobContainer.VideosToConvert);
        publishedVideosBlobs = factory.GetBlobDataService(BlobContainer.Videos);
        this.danceDbContext = danceDbContext;
    }

    public async Task<VideoToTranform?> GetNextVideoToTransformAsync()
    {
        var video = await danceDbContext.VideosToTranform
            .Where(r => r.LockedTill == null || r.LockedTill < DateTime.UtcNow)
            .OrderByDescending(r => r.SharedDateTime)
            .FirstOrDefaultAsync();

        if (video == null)
            return null;

        video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
        
        await danceDbContext.SaveChangesAsync();

        return video;
    }

    public async Task<bool> UpdateVideoToTransformInformationAsync(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata)
    {
        var video = await danceDbContext.VideosToTranform.FirstOrDefaultAsync(r => r.Id == videoId);

        if (video == null)
        {
            return false;
        }

        video.Duration = duration;
        video.RecordedDateTime = DateTime.SpecifyKind(recorded, DateTimeKind.Utc);
        video.Metadata = metadata;

        await danceDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, Stream data)
    {
        var video = await danceDbContext.VideosToTranform.FirstOrDefaultAsync(r => r.Id == videoToConvertId);
        if (video == null)
        {
            return null;
        }

        var newId = Guid.NewGuid();
        await publishedVideosBlobs.Upload(newId.ToString(), data);

        var newVideo = new Video()
        {
            Id = newId,
            BlobId = newId.ToString(),
            Duration = video.Duration,
            RecordedDateTime = video.RecordedDateTime,
            SharedDateTime = video.SharedDateTime,
            UploadedBy = video.UploadedBy,
            Name = video.Name,
            SharedWith = new[]
            {
                ToSharedWith(video, newId)
            }
        };

        danceDbContext.Videos.Add(newVideo);
        
        if (video.Metadata != null)
        {
            danceDbContext.VideoMetadata.Add(new VideoMetadata()
            {
                Metadata = video.Metadata,
                VideoId = newId,
            });
        }

        video.LockedTill = DateTime.SpecifyKind(new DateTime(2090, 01, 01), DateTimeKind.Utc);

        await danceDbContext.SaveChangesAsync();

        return video.Id;
    }

    private SharedWith ToSharedWith(VideoToTranform videoToTranform, Guid newVideo)
    {
        var entity = new SharedWith() { UserId = videoToTranform.UploadedBy, VideoId = newVideo };

        if (videoToTranform.AssignedToEvent)
            entity.EventId = videoToTranform.SharedWithId;
        else
            entity.GroupId = videoToTranform.SharedWithId;

        return entity;
    }

    public Uri GetVideoSas(string blobId)
    {
        var sas = videosToConvertBlobs.GetSas(blobId);
        return sas;
    }

    public SharedBlob GetSasUri()
    {
        return videosToConvertBlobs.CreateUploadSas();
    }
}