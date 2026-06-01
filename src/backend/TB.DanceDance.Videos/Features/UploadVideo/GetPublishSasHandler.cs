using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Infrastructure.Models;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.UploadVideo;

internal class GetPublishSasHandler : IRequestHandler<GetPublishSasQuery, SharedBlob?>
{
    private readonly VideosDbContext dbContext;
    private readonly IBlobDataService publishedBlobs;

    public GetPublishSasHandler(VideosDbContext dbContext, IBlobDataServiceFactory blobFactory)
    {
        this.dbContext = dbContext;
        publishedBlobs = blobFactory.GetBlobDataService(BlobContainer.Videos);
    }

    public async Task<SharedBlob?> HandleAsync(GetPublishSasQuery request, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.Videos.FirstOrDefaultAsync(r => r.Id == request.VideoId, cancellationToken);
        if (video == null)
            return null;

        video.BlobId = Guid.NewGuid().ToString();
        var sas = publishedBlobs.GetUploadSas(video.BlobId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return sas;
    }
}
