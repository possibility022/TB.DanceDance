using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos;

public class VideosData
{
    private readonly VideosDbContext dbContext;

    public VideosData(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<IReadOnlyCollection<Video>> GetUserPrivateVideos(string userId, CancellationToken cancellationToken)
    {
        var privateVideos = dbContext.SharedWith
            .Where(r => r.EventId == null && r.GroupId == null && r.UserId == userId)
            .Join(dbContext.Videos, v => v.VideoId, v => v.Id, (c, v) => v)
            .AsNoTracking();

        var result = await privateVideos.ToArrayAsync(cancellationToken);
        return result;
    }
    
    
}