using Domain;
using Domain.Entities;
using Domain.Models;
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

    public async Task<Video?> GetNextVideoToTransformAsync(CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos
            .Where(r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted == false)
            .OrderByDescending(r => r.SharedDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (video == null)
            return null;

        video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);

        await danceDbContext.SaveChangesAsync(cancellationToken);

        return video;
    }

    public async Task<bool> UpdateVideoInformation(Guid videoId, TimeSpan duration, DateTime recorded,
        byte[]? metadata, CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId, cancellationToken);

        if (video == null)
            return false;

        video.Duration = duration;
        video.RecordedDateTime = DateTime.SpecifyKind(recorded, DateTimeKind.Utc);

        if (metadata != null)
        {
            danceDbContext.VideoMetadata.Add(new VideoMetadata() { Metadata = metadata, VideoId = video.Id, });
        }

        await danceDbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<Guid?> UploadConvertedVideoAsync(Guid videoToConvertId, CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoToConvertId, cancellationToken: cancellationToken);
        if (video == null)
            return null;

        if (video.BlobId == null)
            return null;

        var videoAlreadyUploaded = await publishedVideosBlobs.BlobExistsAsync(video.BlobId);

        if (!videoAlreadyUploaded)
            return null;

        // Calculate and store blob sizes for storage quota tracking
        try
        {
            video.SourceBlobSize = await videosToConvertBlobs.GetBlobSizeAsync(video.SourceBlobId);
            video.ConvertedBlobSize = await publishedVideosBlobs.GetBlobSizeAsync(video.BlobId);
        }
        catch (Exception)
        {
            // If size calculation fails, leave as 0 and continue
            // Sizes can be recalculated later if needed
        }

        video.Converted = true;
        await danceDbContext.SaveChangesAsync(cancellationToken);

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

    public async Task<SharedBlob?> GetSasForConvertedVideoAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId,cancellationToken);
        if (video == null)
            return null;

        video.BlobId = Guid.NewGuid().ToString();

        var sas = publishedVideosBlobs.GetUploadSas(video.BlobId);

        await danceDbContext.SaveChangesAsync(cancellationToken);


        return sas;
    }
}