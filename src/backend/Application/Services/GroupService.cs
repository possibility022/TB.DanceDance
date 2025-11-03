using Domain.Entities;
using Domain.Services;

namespace Application.Services;

public class GroupService : IGroupService
{
    private readonly IApplicationContext dbContext;

    public GroupService(IApplicationContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IQueryable<VideoFromGroupInfo> GetUserVideosForGroup(string userId, Guid groupId)
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
                    Video = video
                };

        return q;
    }

    public IQueryable<VideoFromGroupInfo> GetUserVideosForAllGroups(string userId)
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
                    Video = video
                };

        return q;
    }
}
