namespace Application.Features.Videos;

public interface IThumbnailUrlService
{
    string? GetThumbnailUrl(string? thumbnailBlobId);
}
