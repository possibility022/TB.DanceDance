using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.ViewVideo;

/// <summary>
/// Returns the video for the given blob only if the requesting user is allowed to view it.
/// The access check is the gate; the blob layer itself is unauthenticated.
/// </summary>
class GetVideoForViewingHandler : IRequestHandler<GetVideoForViewingQuery, VideoDto?>
{
    private readonly VideosDbContext dbContext;
    private readonly IRequestHandler<DoesUserHaveAccessToVideoByBlobQuery, bool> doesUserHaveAccessToVideoByBlobQuery;

    public GetVideoForViewingHandler(VideosDbContext dbContext, IRequestHandler<DoesUserHaveAccessToVideoByBlobQuery, bool> doesUserHaveAccessToVideoByBlobQuery)
    {
        this.dbContext = dbContext;
        this.doesUserHaveAccessToVideoByBlobQuery = doesUserHaveAccessToVideoByBlobQuery;
    }

    public async Task<VideoDto?> HandleAsync(GetVideoForViewingQuery request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await doesUserHaveAccessToVideoByBlobQuery.HandleAsync(
            new DoesUserHaveAccessToVideoByBlobQuery(request.UserId, request.BlobId), cancellationToken);

        if (!hasAccess)
            return null;

        return await dbContext.Videos
            .Where(v => v.BlobId == request.BlobId)
            .Select(v => new VideoDto
            {
                Id = v.Id,
                BlobId = v.BlobId!,
                Name = v.Name,
                RecordedDateTime = v.RecordedDateTime,
                Duration = v.Duration,
                Converted = v.Converted,
                CommentVisibility = (int)v.CommentVisibility,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
