using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class VideoUploaderService : IVideoUploaderService
{
    private readonly IBlobDataService videosToConvertBlobs;
    private readonly IBlobDataService publishedVideosBlobs;
    private readonly IApplicationContext danceDbContext;

    public VideoUploaderService(IBlobDataServiceFactory factory, IApplicationContext danceDbContext)
    {
        videosToConvertBlobs = factory.GetBlobDataService(BlobContainer.VideosToConvert);
        publishedVideosBlobs = factory.GetBlobDataService(BlobContainer.Videos);
        this.danceDbContext = danceDbContext;
    }

    public async Task<Video?> GetNextVideoToTransformAsync()
    {
        var video = await danceDbContext.Videos
            .Where(
                r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted == false)
            .OrderByDescending(r => r.SharedDateTime)
            .FirstOrDefaultAsync();

        if (video == null)
            return null;

        video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);

        await danceDbContext.SaveChangesAsync();

        return video;
    }

    public async Task<bool> UpdateVideoInformations(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId);

        if (video == null)
            return false;

        video.Duration = duration;
        video.RecordedDateTime = DateTime.SpecifyKind(recorded, DateTimeKind.Utc);

        if (metadata != null)
        {
            danceDbContext.VideoMetadata.Add(new VideoMetadata()
            {
                Metadata = metadata,
                VideoId = video.Id,
            });
        }

        await danceDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoToConvertId);
        if (video == null)
            return null;

        var videoAlreadyUploaded = await publishedVideosBlobs.BlobExistsAsync(video.BlobId);

        if (!videoAlreadyUploaded)
            return null;

        video.Converted = true;
        await danceDbContext.SaveChangesAsync();

        return video.Id;
    }

    public Uri GetVideoSas(string blobId)
    {
        var sas = videosToConvertBlobs.GetReadSas(blobId);
        return sas;
    }

    public SharedBlob GetUploadSasUri()
    {
        return videosToConvertBlobs.GetUploadSas();
    }
    
    public SharedBlob GetUploadSasUri(string blobId)
    {
        if (string.IsNullOrWhiteSpace(blobId))
            throw new ArgumentNullException(nameof(blobId));
        
        return videosToConvertBlobs.GetUploadSas(blobId);
    }

    public async Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId);
        if (video == null)
            return null;

        video.BlobId = Guid.NewGuid().ToString();

        var sas = publishedVideosBlobs.GetUploadSas(video.BlobId);

        await danceDbContext.SaveChangesAsync();


        return sas;
    }
}