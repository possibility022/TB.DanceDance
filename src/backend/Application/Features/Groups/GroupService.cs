using Application.Domain.Models;
using Application.Extensions;
using Application.Features.Videos;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using Group = Domain.Entities.Group;

namespace Application.Features.Groups;

public class GroupService : IGroupService
{
    private readonly IApplicationContext dbContext;
    private readonly IThumbnailUrlService thumbnailUrlService;

    public GroupService(IApplicationContext dbContext, IThumbnailUrlService thumbnailUrlService)
    {
        this.dbContext = dbContext;
        this.thumbnailUrlService = thumbnailUrlService;
    }

    public async Task<ICollection<Group>> GetAllGroups(CancellationToken cancellationToken)
    {
        return await dbContext.Groups.ToListAsync(cancellationToken);
    }
    
    [Obsolete]
    public Task<VideoFromGroupInfo[]> GetUserVideosForGroup(string userId, Guid groupId, CancellationToken cancellationToken)
    {
        var q = from danceGroup in dbContext.Groups
                join assignedTo in dbContext.AssingedToGroups on danceGroup.Id equals assignedTo.GroupId
                join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime && danceGroup.Id == groupId
                orderby video.RecordedDateTime descending
                select new VideoFromGroupInfo()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    Video = video,
                    Group = danceGroup
                };

        return q.ToArrayAsync(cancellationToken);
    }

    [Obsolete]
    public Task<VideoFromGroupInfo[]> GetUserVideosForAllGroups(string userId, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
                join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
                join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime
                orderby video.RecordedDateTime descending
                select new VideoFromGroupInfo()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    Video = video,
                    Group = danceGroup
                };

        return q.ToArrayAsync(cancellationToken);
    }

    public async Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime
            orderby video.RecordedDateTime descending
            select new
            {
                Information = new VideoFromGroupInformation()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    BlobId = video.BlobId ?? string.Empty,
                    CommentVisibility = (int)video.CommentVisibility,
                    Duration = video.Duration,
                    Name = video.Name,
                    RecordedDateTime = video.RecordedDateTime,
                    Converted = video.Converted,
                    VideoId = video.Id,
                    IsOwner = video.UploadedBy == userId,
                },
                video.ThumbnailBlobId
            };

        var (rows, totalCount) = await q.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        var items = rows.Select(r =>
        {
            r.Information.ThumbnailUrl = thumbnailUrlService.GetThumbnailUrl(r.ThumbnailBlobId);
            return r.Information;
        }).ToArray();

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<VideoFromGroupInformation> Items, int TotalCount)> GetAllVideos(string userId, Guid groupId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime && assignedTo.GroupId == groupId
            orderby video.RecordedDateTime descending
            select new
            {
                Information = new VideoFromGroupInformation()
                {
                    GroupId = danceGroup.Id,
                    GroupName = danceGroup.Name,
                    BlobId = video.BlobId ?? string.Empty,
                    CommentVisibility = (int)video.CommentVisibility,
                    Duration = video.Duration,
                    Name = video.Name,
                    RecordedDateTime = video.RecordedDateTime,
                    Converted = video.Converted,
                    VideoId = video.Id,
                    IsOwner = video.UploadedBy == userId,
                },
                video.ThumbnailBlobId
            };

        var (rows, totalCount) = await q.ToPagedResultAsync(pageNumber, pageSize, cancellationToken);
        var items = rows.Select(r =>
        {
            r.Information.ThumbnailUrl = thumbnailUrlService.GetThumbnailUrl(r.ThumbnailBlobId);
            return r.Information;
        }).ToArray();

        return (items, totalCount);
    }
}
