using Domain;
using Domain.Models;
using Domain.Services;

namespace Application.Features.Videos;

public class ThumbnailUrlService : IThumbnailUrlService
{
    private static readonly TimeSpan BucketSize = TimeSpan.FromMinutes(30);

    private readonly IBlobDataService thumbnailBlobService;

    public ThumbnailUrlService(IBlobDataServiceFactory blobServiceFactory)
    {
        thumbnailBlobService = blobServiceFactory.GetBlobDataService(BlobContainer.Thumbnails);
    }

    public string? GetThumbnailUrl(string? thumbnailBlobId)
    {
        if (string.IsNullOrEmpty(thumbnailBlobId))
            return null;

        var expiresOn = SasExpiry.QuantizeToNextBoundary(DateTimeOffset.UtcNow, BucketSize);
        return thumbnailBlobService.GetReadSas(thumbnailBlobId, expiresOn).ToString();
    }
}
