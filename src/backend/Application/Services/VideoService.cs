using Domain;
using Domain.Entities;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Requests;

namespace Application.Services;

public class VideoService : IVideoService
{
    private readonly IApplicationContext dbContext;
    private readonly IBlobDataService blobService;
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
            UploadedBy = userId,
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
}