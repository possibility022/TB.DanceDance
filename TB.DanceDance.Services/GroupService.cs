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

    public IQueryable<VideoInfo> GetUserVideosFromGroups(string userId)
    {
        var q = from assignedTo in dbContext.AssingedToGroups
                join sharedWith in dbContext.SharedWith on assignedTo.GroupId equals sharedWith.GroupId
                join video in dbContext.Videos on sharedWith.VideoId equals video.Id
                where assignedTo.UserId == userId
                orderby video.RecordedDateTime descending
                select new VideoInfo()
                {
                    SharedWithEvent = true,
                    SharedWithGroup = false,
                    Video = video
                };

        return q;
    }
}
