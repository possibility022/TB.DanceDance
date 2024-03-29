﻿using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class VideoService : IVideoService
{
    private readonly IApplicationContext dbContext;
    private readonly IBlobDataService blobService;
    private readonly IVideoUploaderService videoUploaderService;

    public VideoService(
        IApplicationContext dbContext,
        IBlobDataServiceFactory blobServiceFactory,
        IVideoUploaderService videoUploaderService)
    {
        this.dbContext = dbContext;
        blobService = blobServiceFactory.GetBlobDataService(BlobContainer.Videos);
        this.videoUploaderService = videoUploaderService;
    }

    private IQueryable<Video> GetBaseVideosForUserQuery(string userId)
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
               sharedWith.UserId == userId || eventsAssignments.UserId == userId || groupsAssignments.UserId == userId && groupsAssignments.WhenJoined < video.RecordedDateTime
               orderby video.RecordedDateTime descending
               select video;
    }

    public async Task<bool> DoesUserHasAccessAsync(string videoBlobId, string userId)
    {
        var query = GetBaseVideosForUserQuery(userId)
            .Where(v => v.BlobId == videoBlobId)
            .AnyAsync();

        var any = await query;

        return any;
    }

    public Task<Video?> GetVideoByBlobAsync(string userId, string blobId)
    {
        return GetBaseVideosForUserQuery(userId)
            .Where(r => r.BlobId == blobId)
            .FirstOrDefaultAsync();
    }

    public Task<Stream> OpenStream(string blobName)
    {
        return blobService.OpenStream(blobName);
    }

    public async Task<bool> RenameVideoAsync(Guid guid, string newName)
    {
        var video = await dbContext.Videos.FirstAsync(r => r.Id == guid);

        if (video == null)
            return false;

        video.Name = newName;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<SharedBlob> GetSharingLink(string userId, string name, string fileName, bool assignedToEvent, Guid sharedWith)
    {
        var sharedBlob = videoUploaderService.GetSasUri();

        var video = new Video()
        {
            FileName = fileName,
            SourceBlobId = sharedBlob.Name,
            Name = name,
            UploadedBy = userId,
            Duration = null,
            RecordedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            SharedDateTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            SharedWith = new[] {
                new SharedWith()
                {
                    VideoId = default, // should be set by EF
                    UserId = userId,
                    EventId = assignedToEvent ? sharedWith : null,
                    GroupId = assignedToEvent ? null : sharedWith
                }
            },
            Converted = false
        };

        dbContext.Videos.Add(video);
        await dbContext.SaveChangesAsync();

        return sharedBlob;
    }
}