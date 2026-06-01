using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.UploadVideo;

internal class UploadConvertedVideoHandler : IRequestHandler<UploadConvertedVideoCommand, Guid?>
{
    private readonly VideosDbContext dbContext;
    private readonly IBlobDataService toConvertBlobs;
    private readonly IBlobDataService publishedBlobs;

    public UploadConvertedVideoHandler(VideosDbContext dbContext, IBlobDataServiceFactory blobFactory)
    {
        this.dbContext = dbContext;
        toConvertBlobs = blobFactory.GetBlobDataService(BlobContainer.VideosToConvert);
        publishedBlobs = blobFactory.GetBlobDataService(BlobContainer.Videos);
    }

    public async Task<Guid?> HandleAsync(UploadConvertedVideoCommand request, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == request.VideoId, cancellationToken);
        if (video == null)
            return null;

        if (video.BlobId == null)
            return null;

        var videoAlreadyUploaded = await publishedBlobs.BlobExistsAsync(video.BlobId);
        if (!videoAlreadyUploaded)
            return null;

        try
        {
            video.SourceBlobSize = await toConvertBlobs.GetBlobSizeAsync(video.SourceBlobId);
            video.ConvertedBlobSize = await publishedBlobs.GetBlobSizeAsync(video.BlobId);
        }
        catch (Exception)
        {
            // Sizes can be recalculated later if needed
        }

        video.Converted = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return video.Id;
    }
}
