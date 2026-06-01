using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.Sharing;

public class CreateSharingLinkCommandHandler : IRequestHandler<CreateSharingLinkCommand, UploadContext?>
{
    private readonly VideosDbContext videosDbContext;
    private readonly IBlobDataServiceFactory blobDataServiceFactory;

    public CreateSharingLinkCommandHandler(VideosDbContext videosDbContext, IBlobDataServiceFactory blobDataServiceFactory)
    {
        this.videosDbContext = videosDbContext;
        this.blobDataServiceFactory = blobDataServiceFactory;
    }

    public async Task<UploadContext?> HandleAsync(CreateSharingLinkCommand request, CancellationToken cancellationToken = default)
    {
        var video = await videosDbContext.Videos.FirstOrDefaultAsync(r => r.Id == request.VideoId, cancellationToken);
        if (video is null)
        {
            return null;
        }

        var blobContainer = blobDataServiceFactory.GetBlobDataService(BlobContainer.Videos);

        var sas = blobContainer.GetUploadSas(video.SourceBlobId);
        
        return new UploadContext()
        {
            Sas = sas.Sas,
            VideoId = video.Id,
            SourceBlobId = video.SourceBlobId,
            ExpireAt = sas.ExpiresAt
        };
    }
}