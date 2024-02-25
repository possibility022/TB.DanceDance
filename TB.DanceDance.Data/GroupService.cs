using TB.DanceDance.Data.PostgreSQL;
using TB.DanceDance.Services.Models;

namespace TB.DanceDance.Services;

public class GroupService : IGroupService
{
    private readonly DanceDbContext dbContext;

    public GroupService(DanceDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public IQueryable<VideoFromGroupInfo> GetUserVideosFromGroups(string userId)
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
