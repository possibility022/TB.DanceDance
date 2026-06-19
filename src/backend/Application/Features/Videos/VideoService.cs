using Application.Features.AccessManagement;
using Application.Features.Videos.Endpoints.Videos;
using Domain;
using Domain.Entities;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Features.Videos;

namespace Application.Features.Videos;

public class VideoService : IVideoService
{
    private readonly IApplicationContext dbContext;
    private readonly IBlobDataService blobService;
    private readonly IBlobDataService sourceBlobService;
    private readonly IBlobDataService thumbnailBlobService;
    private readonly IVideoUploaderService videoUploaderService;
    private readonly IAccessService accessService;

    public VideoService(
        IApplicationContext dbContext,
        IBlobDataServiceFactory blobServiceFactory,
        IVideoUploaderService videoUploaderService,
        IAccessService accessService
        )
    {
        this.dbContext = dbContext;
        blobService = blobServiceFactory.GetBlobDataService(BlobContainer.Videos);
        sourceBlobService = blobServiceFactory.GetBlobDataService(BlobContainer.VideosToConvert);
        thumbnailBlobService = blobServiceFactory.GetBlobDataService(BlobContainer.Thumbnails);
        this.videoUploaderService = videoUploaderService;
        this.accessService = accessService;
    }

    public async Task<Video?> GetVideoByBlobAsync(string userId, string blobId, CancellationToken cancellationToken)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(blobId, userId, cancellationToken);
        if (!hasAccess)
            return null;

        return await dbContext.Videos.Where(r => r.BlobId == blobId).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Stream> OpenStream(string blobName, CancellationToken cancellationToken)
    {
        return blobService.OpenStream(blobName, cancellationToken);
    }

    public async Task<string> GetContentType(string blobName, CancellationToken cancellationToken)
    {
        var contentType = await blobService.GetContentTypeAsync(blobName, cancellationToken);
        return contentType ?? "video/webm";
    }

    public async Task<bool> RenameVideoAsync(Guid guid, string newName, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == guid, cancellationToken: cancellationToken);

        if (video == null)
            return false;

        video.Name = newName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UploadContext?> GetSharingLink(Guid videoId, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId, cancellationToken: cancellationToken);
        
        if (video is null)
            return null;

        var sas = videoUploaderService.GetUploadSasUri(video.SourceBlobId);

        return new UploadContext()
        {
            Sas = sas.Sas,
            VideoId = video.Id,
            SourceBlobId = video.SourceBlobId,
            ExpireAt = sas.ExpiresAt
        };
    }

    public async Task<UploadContext> GetSharingLink(string userId, string name, string fileName, SharingWithType sharingWithType, Guid? sharedWith, CancellationToken cancellationToken)
    {
        var sas = videoUploaderService.GetUploadSasUri();

        Guid? eventId = null;
        Guid? groupId = null;

        // Determine EventId and GroupId based on SharingWithType
        switch (sharingWithType)
        {
            case SharingWithType.Event:
                eventId = sharedWith;
                break;
            case SharingWithType.Group:
                groupId = sharedWith;
                break;
            case SharingWithType.Private:
                // Both EventId and GroupId remain null for private videos
                break;
            default:
                throw new ArgumentException($"Invalid SharingWithType: {sharingWithType}", nameof(sharingWithType));
        }

        var video = new Video()
        {
            FileName = fileName,
            SourceBlobId = sas.BlobId,
            Name = name,
            OwnerUserId = userId,
            UploadedByUserId = userId,
            Duration = null,
            RecordedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            SharedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            SharedWith =
            [
                new SharedWith()
                {
                    VideoId = Guid.Empty, // should be set by EF
                    UserId = userId,
                    EventId = eventId,
                    GroupId = groupId
                }
            ],
            Converted = false
        };

        dbContext.Videos.Add(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UploadContext()
        {
            Sas = sas.Sas,
            SourceBlobId = video.SourceBlobId,
            VideoId = video.Id,
            ExpireAt = sas.ExpiresAt
        };
    }

    public async Task<bool> UpdateCommentVisibilityAsync(
        Guid videoId,
        string userId,
        CommentVisibility commentVisibility,
        CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
        {
            return false;
        }

        // Only the video owner can update comment visibility
        if (video.OwnerUserId != userId)
        {
            return false;
        }

        video.CommentVisibility = commentVisibility;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DeleteVideoResult> DeleteVideoAsync(Guid videoId, string userId, CancellationToken cancellationToken)
    {
        var video = await dbContext.Videos
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken);

        if (video == null)
            return DeleteVideoResult.NotFound;

        // Only the uploader may delete — stricter than the access-based rename path.
        if (video.OwnerUserId != userId)
            return DeleteVideoResult.Forbidden;

        // A video that was transferred to this user less than RollbackWindowDays ago can still be
        // reclaimed by the previous owner — block deletion until that window passes or a rollback happens.
        var rollbackCutoff = DateTimeOffset.UtcNow.AddDays(-VideoTransfer.RollbackWindowDays);
        var isWithinRollbackWindow = await dbContext.VideoTransferItems
            .AnyAsync(i => i.VideoId == videoId
                        && i.Transfer.Status == TransferStatus.Accepted
                        && i.Transfer.AcceptedAt > rollbackCutoff, cancellationToken);
        if (isWithinRollbackWindow)
            return DeleteVideoResult.RollbackPending;

        // Capture blob ids before the row is removed.
        var sourceBlobId = video.SourceBlobId;
        var convertedBlobId = video.BlobId;
        var thumbnailBlobId = video.ThumbnailBlobId;

        // Removing the Video row cascades to its SharedWith / Comment / SharedLink / VideoMetadata
        // rows via the configured ON DELETE CASCADE foreign keys.
        dbContext.Videos.Remove(video);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Blobs live outside the database, so clean them up explicitly. The three blobs live in
        // separate containers; DeleteAsync is a no-op when a blob is missing (e.g. not yet converted).
        await sourceBlobService.DeleteAsync(sourceBlobId, cancellationToken);
        if (!string.IsNullOrEmpty(convertedBlobId))
            await blobService.DeleteAsync(convertedBlobId, cancellationToken);
        if (!string.IsNullOrEmpty(thumbnailBlobId))
            await thumbnailBlobService.DeleteAsync(thumbnailBlobId, cancellationToken);

        return DeleteVideoResult.Deleted;
    }
}