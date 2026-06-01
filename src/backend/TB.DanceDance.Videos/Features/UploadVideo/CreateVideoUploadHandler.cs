using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;
using TB.DanceDance.Videos.Domain.Entities;
using TB.DanceDance.Videos.Infrastructure;

namespace TB.DanceDance.Videos.Features.UploadVideo;

public class CreateVideoUploadHandler : IRequestHandler<CreateVideoUploadCommand, UploadContext?>
{
    private readonly VideosDbContext videosDbContext;
    private readonly IBlobDataServiceFactory blobDataServiceFactory;

    public CreateVideoUploadHandler(VideosDbContext videosDbContext, IBlobDataServiceFactory blobDataServiceFactory)
    {
        this.videosDbContext = videosDbContext;
        this.blobDataServiceFactory = blobDataServiceFactory;
    }

    public async Task<UploadContext?> HandleAsync(CreateVideoUploadCommand request, CancellationToken cancellationToken = default)
    {
        var toConvertBlobService = blobDataServiceFactory.GetBlobDataService(BlobContainer.VideosToConvert);
        var sas = toConvertBlobService.GetUploadSas();

        Guid? eventId = null;
        Guid? groupId = null;

        // Determine EventId and GroupId based on SharingWithType
        switch (request.SharingWithType)
        {
            case SharingWithType.Event:
                eventId = request.SharedWith;
                break;
            case SharingWithType.Group:
                groupId = request.SharedWith;
                break;
            case SharingWithType.Private:
                // Both EventId and GroupId remain null for private videos
                break;
            default:
                throw new ArgumentException($"Invalid SharingWithType: {request.SharingWithType}", nameof(request));
        }

        var video = Video.Factory.CreateForUpload(
            name: request.Name,
            fileName: request.FileName,
            sourceBlobId: sas.BlobId,
            userId: request.UserId,
            sharedWith: [SharedWith.Factory.Create(request.UserId, eventId, groupId)]);

        videosDbContext.Videos.Add(video);
        await videosDbContext.SaveChangesAsync(cancellationToken);

        return new UploadContext()
        {
            Sas = sas.Sas,
            SourceBlobId = video.SourceBlobId,
            VideoId = video.Id,
            ExpireAt = sas.ExpiresAt
        };
    }
}
