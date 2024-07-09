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

    public async Task<Video?> GetNextVideoToTransformAsync(CancellationToken token)
    {
        var video = await danceDbContext.Videos
            .Where(
                r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted == false)
            .OrderByDescending(r => r.SharedDateTime)
            .FirstOrDefaultAsync(token);

        if (video == null)
            return null;

        video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);

        await danceDbContext.SaveChangesAsync(token);

        return video;
    }

    public async Task<bool> UpdateVideoInformations(Guid videoId, TimeSpan duration, DateTime recorded, byte[]? metadata, CancellationToken token)
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

        await danceDbContext.SaveChangesAsync(token);

        return true;
    }

    public async Task<Guid?> PublishConvertedVideo(Guid videoToConvertId, CancellationToken token)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoToConvertId, token);
        if (video == null)
            return null;

        if (video.BlobId == null)
        {
            throw new ApplicationException("Blob id is null for converted video. Video id:" + video.Id)
        }

        var videoAlreadyUploaded = await publishedVideosBlobs.BlobExistsAsync(video.BlobId, token);

        if (!videoAlreadyUploaded)
            return null;

        video.Converted = true;
        await danceDbContext.SaveChangesAsync();

        return video.Id;
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

    public async Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId, CancellationToken token)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId);
        if (video == null)
            return null;

        video.BlobId = Guid.NewGuid().ToString();

        var sas = publishedVideosBlobs.CreateUploadSas(video.BlobId);

        await danceDbContext.SaveChangesAsync();


        return sas;
    }
}