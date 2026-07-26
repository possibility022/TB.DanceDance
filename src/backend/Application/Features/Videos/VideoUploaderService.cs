using Domain;
using Domain.Entities;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Videos;

public class VideoUploaderService : IVideoUploaderService
{
    private readonly IBlobDataService videosToConvertBlobs;
    private readonly IBlobDataService publishedVideosBlobs;
    private readonly IBlobDataService thumbnailBlobs;
    private readonly IApplicationContext danceDbContext;
    private readonly ILogger<VideoUploaderService> logger;

    public VideoUploaderService(
        IBlobDataServiceFactory factory,
        IApplicationContext danceDbContext,
        ILogger<VideoUploaderService> logger)
    {
        videosToConvertBlobs = factory.GetBlobDataService(BlobContainer.VideosToConvert);
        publishedVideosBlobs = factory.GetBlobDataService(BlobContainer.Videos);
        thumbnailBlobs = factory.GetBlobDataService(BlobContainer.Thumbnails);
        this.danceDbContext = danceDbContext;
        this.logger = logger;
    }

    public async Task<Video?> GetNextVideoToTransformAsync(CancellationToken cancellationToken)
    {
        var candidates = await danceDbContext.Videos
            .Where(r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted == false)
            .OrderByDescending(r => r.SharedDateTime)
            .ToListAsync(cancellationToken);

        foreach (var video in candidates)
        {
            // A block blob is only visible once its block list is committed; an in-progress or
            // interrupted upload reports as non-existent. So existence == fully uploaded.
            if (!await videosToConvertBlobs.BlobExistsAsync(video.SourceBlobId))
                continue; // not fully uploaded yet — leave unlocked so it's retried next poll

            video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
            await danceDbContext.SaveChangesAsync(cancellationToken);
            return video;
        }

        return null;
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
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId, cancellationToken);
        if (video == null)
            return null;

        video.BlobId = Guid.NewGuid().ToString();

        var sas = publishedVideosBlobs.GetUploadSas(video.BlobId);

        await danceDbContext.SaveChangesAsync(cancellationToken);

        return sas;
    }

    public async Task<(Guid Id, string BlobId, string FileName, Uri Sas)?> GetNextVideoForThumbnailAsync(CancellationToken cancellationToken)
    {
        var candidates = await danceDbContext.Videos
            .Where(r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted && r.ThumbnailBlobId == null && r.BlobId != null)
            .OrderByDescending(r => r.SharedDateTime)
            .ToListAsync(cancellationToken);

        foreach (var video in candidates)
        {
            try
            {
                var sas = await ResolveThumbnailSourceSasAsync(video);
                if (sas is null)
                    continue;

                video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
                await danceDbContext.SaveChangesAsync(cancellationToken);
                return (video.Id, video.BlobId!, video.FileName, sas);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to prepare thumbnail source for video {VideoId}; skipping without locking",
                    video.Id);
            }
        }

        return null;
    }

    /// <summary>
    /// Prefers the original upload blob; falls back to the converted video when the source is gone.
    /// Returns null when neither blob exists so the caller can try the next candidate.
    /// </summary>
    private async Task<Uri?> ResolveThumbnailSourceSasAsync(Video video)
    {
        if (await videosToConvertBlobs.BlobExistsAsync(video.SourceBlobId))
            return videosToConvertBlobs.GetReadSas(video.SourceBlobId);

        logger.LogWarning(
            "Source blob {SourceBlobId} missing for video {VideoId}; falling back to converted blob {BlobId}",
            video.SourceBlobId, video.Id, video.BlobId);

        if (await publishedVideosBlobs.BlobExistsAsync(video.BlobId!))
            return publishedVideosBlobs.GetReadSas(video.BlobId!);

        logger.LogWarning(
            "Skipping thumbnail for video {VideoId}: source blob {SourceBlobId} and converted blob {BlobId} are both missing",
            video.Id, video.SourceBlobId, video.BlobId);
        return null;
    }

    public async Task<SharedBlob?> GetSasForThumbnailUploadAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId, cancellationToken);
        if (video == null)
            return null;

        var blobId = $"{videoId}/thumbnail.jpg";
        var sas = thumbnailBlobs.GetUploadSas(blobId);
        return sas;
    }

    public async Task<bool> PublishThumbnailAsync(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await danceDbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId, cancellationToken);
        if (video == null)
            return false;

        var blobId = $"{videoId}/thumbnail.jpg";
        var exists = await thumbnailBlobs.BlobExistsAsync(blobId);
        if (!exists)
            return false;

        video.ThumbnailBlobId = blobId;
        await danceDbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}