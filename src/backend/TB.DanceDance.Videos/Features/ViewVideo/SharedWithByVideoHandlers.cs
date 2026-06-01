using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.ViewVideo;

/// <summary>
/// Resolves the <see cref="SharedWithResponse"/> rows for a video, identified either by its
/// blob id or its video id. These are the local (Videos module) facts the access-decision
/// orchestration consumes; the group/event grant decision itself is delegated to the Access
/// module via <c>DoesUserHasAccessToSharedWith</c>.
/// </summary>
class SharedWithByVideoHandlers
    : IRequestHandler<SharedWithByVideoBlobIdQuery, IReadOnlyCollection<SharedWithResponse>>,
      IRequestHandler<SharedWithByVideoIdQuery, IReadOnlyCollection<SharedWithResponse>>
{
    private readonly VideosDbContext dbContext;

    public SharedWithByVideoHandlers(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SharedWithResponse>> HandleAsync(SharedWithByVideoBlobIdQuery request, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.SharedWith
            .Where(sw => sw.Video.BlobId == request.VideoBlobId)
            .Select(sw => new SharedWithResponse
            {
                VideoId = sw.VideoId,
                UserId = sw.UserId,
                EventId = sw.EventId,
                GroupId = sw.GroupId,
            })
            .ToArrayAsync(cancellationToken);

        return rows.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<SharedWithResponse>> HandleAsync(SharedWithByVideoIdQuery request, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.SharedWith
            .Where(sw => sw.VideoId == request.VideoId)
            .Select(sw => new SharedWithResponse
            {
                VideoId = sw.VideoId,
                UserId = sw.UserId,
                EventId = sw.EventId,
                GroupId = sw.GroupId,
            })
            .ToArrayAsync(cancellationToken);

        return rows.AsReadOnly();
    }
}
