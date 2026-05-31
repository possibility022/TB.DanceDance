using TB.DanceDance.Utilities.Infrastructure;
using TB.DanceDance.Utilities.Mediating;
using TB.DanceDance.Videos.Contracts;

namespace TB.DanceDance.Videos.ViewVideo;

/// <summary>
/// Opens the blob stream for a converted video. Unauthenticated at the blob layer —
/// callers must run the access check (<see cref="DoesUserHaveAccessToVideoByBlobQuery"/>)
/// before invoking this.
/// </summary>
class OpenVideoStreamHandler : IRequestHandler<OpenVideoStreamQuery, Stream>
{
    private readonly IBlobDataService blobService;

    public OpenVideoStreamHandler(IBlobDataServiceFactory blobDataServiceFactory)
    {
        blobService = blobDataServiceFactory.GetBlobDataService(BlobContainer.Videos);
    }

    public Task<Stream> HandleAsync(OpenVideoStreamQuery request, CancellationToken cancellationToken = default)
    {
        return blobService.OpenStream(request.BlobName, cancellationToken);
    }
}
