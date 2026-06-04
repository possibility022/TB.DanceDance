using Application.Domain.Models;
using Application.Features.Groups.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Groups;

public class GroupService : IGroupService
{
    private readonly IApplicationContext dbContext;

    public GroupService(IApplicationContext dbContext)
    {
        this.dbContext = dbContext;
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

    public Task<VideoFromGroupInformation[]> GetAllVideos(string userId, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime
            orderby video.RecordedDateTime descending
            select new VideoFromGroupInformation()
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
            };

        return q.ToArrayAsync(cancellationToken);
    }
    
    public Task<VideoFromGroupInformation[]> GetAllVideos(string userId, Guid groupId, CancellationToken cancellationToken)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
            join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
            join danceGroup in dbContext.Groups on assignedTo.GroupId equals danceGroup.Id
            join video in dbContext.Videos on sharedWith.VideoId equals video.Id
            where assignedTo.UserId == userId && assignedTo.WhenJoined < video.RecordedDateTime && assignedTo.GroupId == groupId
            orderby video.RecordedDateTime descending
            select new VideoFromGroupInformation()
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
            };

        return q.ToArrayAsync(cancellationToken);
    }
}
