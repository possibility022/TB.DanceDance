using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.ViewVideo;


record ViewVideosFromGroupQuery : IRequest<IReadOnlyCollection<VideoDto>>
{
    public Guid GroupId { get; init; }
}

record ViewVideosFromEventQuery : IRequest<IReadOnlyCollection<VideoDto>>
{
    public Guid EventId { get; init; }
}

class ViewVideos 
    : IRequestHandler<ViewVideosFromGroupQuery, IReadOnlyCollection<VideoDto>>,
      IRequestHandler<ViewVideosFromEventQuery, IReadOnlyCollection<VideoDto>>
{
    private readonly VideosDbContext dbContext;

    public ViewVideos(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewVideosFromGroupQuery request, CancellationToken cancellationToken = default)
    {
        var videos = await dbContext.SharedWith
            .Where(r => r.GroupId!.Value == request.GroupId)
            .Select(sw => sw.Video)
            .Select(v => new VideoDto()
            {
                Name = v.Name,
                Id = v.Id,
                BlobId = v.BlobId!,
                CommentVisibility = (int)v.CommentVisibility,
                Converted = v.Converted,
                Duration = v.Duration,
                RecordedDateTime = v.RecordedDateTime,
            })
            .ToArrayAsync(cancellationToken);
        
        return videos.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<VideoDto>> HandleAsync(ViewVideosFromEventQuery request, CancellationToken cancellationToken = default)
    {
        var videos = await dbContext.SharedWith
            .Where(r => r.EventId!.Value == request.EventId)
            .Select(sw => sw.Video)
            .Select(v => new VideoDto()
            {
                Name = v.Name,
                Id = v.Id,
                BlobId = v.BlobId!,
                CommentVisibility = (int)v.CommentVisibility,
                Converted = v.Converted,
                Duration = v.Duration,
                RecordedDateTime = v.RecordedDateTime,
            })
            .ToArrayAsync(cancellationToken);
        
        return videos.AsReadOnly();
    }
}