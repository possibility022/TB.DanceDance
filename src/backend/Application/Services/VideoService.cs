using Domain;
using Domain.Entities;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Video?> GetVideoByBlobAsync(string userId, string blobId)
    {
        var hasAccess = await accessService.DoesUserHasAccessAsync(blobId, userId);
        if (!hasAccess)
            return null;

        return await dbContext.Videos.Where(r => r.BlobId == blobId).FirstOrDefaultAsync();
    }

    public Task<Stream> OpenStream(string blobName)
    {
        return blobService.OpenStream(blobName);
    }

    public async Task<bool> RenameVideoAsync(Guid guid, string newName)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == guid);

        if (video == null)
            return false;

        video.Name = newName;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<UploadContext?> GetSharingLink(Guid videoId)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == videoId);
        
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

    public async Task<UploadContext> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith)
    {
        var sas = videoUploaderService.GetUploadSasUri();

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
                    EventId = assignedToEvent ? sharedWith : null,
                    GroupId = assignedToEvent ? null : sharedWith
                }
            ],
            Converted = false
        };

        dbContext.Videos.Add(video);
        await dbContext.SaveChangesAsync();

        return new UploadContext()
        {
            Sas = sas.Sas,
            SourceBlobId = video.SourceBlobId,
            VideoId = video.Id,
            ExpireAt = sas.ExpiresAt
        };
    }
}