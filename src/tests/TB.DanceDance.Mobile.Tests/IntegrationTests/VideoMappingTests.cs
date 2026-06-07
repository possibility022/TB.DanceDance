using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Library.Data.Models;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class VideoMappingTests
{
    [Fact]
    public void MapFromApiResponse_MapsThumbnailUrl_FromVideoInformation()
    {
        var videos = new List<VideoInformation>
        {
            new()
            {
                VideoId = Guid.NewGuid(),
                BlobId = "blob-1",
                Name = "Recording",
                RecordedDateTime = DateTime.UtcNow,
                Converted = true,
                ThumbnailUrl = "https://blob.local/thumbnails/blob-1.jpg",
            },
        };

        var result = Video.MapFromApiResponse(videos);

        Assert.Equal("https://blob.local/thumbnails/blob-1.jpg", result.Single().ThumbnailUrl);
    }

    [Fact]
    public void MapFromApiResponse_MapsNullThumbnailUrl_FromVideoInformation()
    {
        var videos = new List<VideoInformation>
        {
            new()
            {
                VideoId = Guid.NewGuid(),
                BlobId = "blob-1",
                Name = "Recording",
                RecordedDateTime = DateTime.UtcNow,
                Converted = true,
                ThumbnailUrl = null,
            },
        };

        var result = Video.MapFromApiResponse(videos);

        Assert.Null(result.Single().ThumbnailUrl);
    }

    [Fact]
    public void MapFromApiResponse_MapsThumbnailUrl_FromVideoFromGroupInformation()
    {
        var videos = new List<VideoFromGroupInformation>
        {
            new()
            {
                VideoId = Guid.NewGuid(),
                BlobId = "blob-2",
                Name = "Group recording",
                RecordedDateTime = DateTime.UtcNow,
                Converted = true,
                GroupId = Guid.NewGuid(),
                GroupName = "Group",
                ThumbnailUrl = "https://blob.local/thumbnails/blob-2.jpg",
            },
        };

        var result = Video.MapFromApiResponse(videos);

        Assert.Equal("https://blob.local/thumbnails/blob-2.jpg", result.Single().ThumbnailUrl);
    }

    [Fact]
    public void MapFromApiResponse_MapsMissingThumbnailUrl_FromVideoFromGroupInformation()
    {
        var videos = new List<VideoFromGroupInformation>
        {
            new()
            {
                VideoId = Guid.NewGuid(),
                BlobId = "blob-2",
                Name = "Group recording",
                RecordedDateTime = DateTime.UtcNow,
                Converted = true,
                GroupId = Guid.NewGuid(),
                GroupName = "Group",
            },
        };

        var result = Video.MapFromApiResponse(videos);

        Assert.Null(result.Single().ThumbnailUrl);
    }
}
