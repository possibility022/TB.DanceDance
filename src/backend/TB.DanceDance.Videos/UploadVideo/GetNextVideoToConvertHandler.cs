using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.UploadVideo;

internal class GetNextVideoToConvertHandler : IRequestHandler<GetNextVideoToConvertQuery, VideoToConvertDto?>
{
    private readonly VideosDbContext dbContext;
    private readonly IBlobDataService toConvertBlobs;

    public GetNextVideoToConvertHandler(VideosDbContext dbContext, IBlobDataServiceFactory blobFactory)
    {
        this.dbContext = dbContext;
        toConvertBlobs = blobFactory.GetBlobDataService(BlobContainer.VideosToConvert);
    }

    public async Task<VideoToConvertDto?> HandleAsync(GetNextVideoToConvertQuery request, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.Videos
            .Where(r => (r.LockedTill == null || r.LockedTill < DateTime.UtcNow) && r.Converted == false)
            .OrderByDescending(r => r.SharedDateTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (video == null)
            return null;

        video.LockedTill = DateTime.SpecifyKind(DateTime.Now.AddDays(1), DateTimeKind.Utc);
        await dbContext.SaveChangesAsync(cancellationToken);

        var sas = toConvertBlobs.GetReadSas(video.SourceBlobId);
        return new VideoToConvertDto(video.Id, video.FileName, sas);
    }
}
