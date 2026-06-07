using Application.Features.Videos;
using Domain;
using Domain.Services;
using NSubstitute;

namespace TB.DanceDance.Tests.Features.Videos;

public class ThumbnailUrlServiceTests
{
    private readonly IBlobDataService blobDataService;
    private readonly ThumbnailUrlService thumbnailUrlService;

    public ThumbnailUrlServiceTests()
    {
        blobDataService = Substitute.For<IBlobDataService>();
        var factory = Substitute.For<IBlobDataServiceFactory>();
        factory.GetBlobDataService(BlobContainer.Thumbnails).Returns(blobDataService);

        thumbnailUrlService = new ThumbnailUrlService(factory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetThumbnailUrl_ReturnsNull_WhenBlobIdIsNullOrEmpty(string? thumbnailBlobId)
    {
        Assert.Null(thumbnailUrlService.GetThumbnailUrl(thumbnailBlobId));
        blobDataService.DidNotReceiveWithAnyArgs().GetReadSas(default!, default);
    }

    [Fact]
    public void GetThumbnailUrl_DelegatesToBlobDataService_WithQuantizedExpiry()
    {
        const string blobId = "thumb-1";
        var sas = new Uri("https://azurite/thumbnails/thumb-1?sv=stub&se=2026-06-07T10%3A30%3A00Z");
        blobDataService.GetReadSas(blobId, Arg.Any<DateTimeOffset>()).Returns(sas);

        var result = thumbnailUrlService.GetThumbnailUrl(blobId);

        Assert.Equal(sas.ToString(), result);
        blobDataService.Received(1).GetReadSas(blobId, Arg.Any<DateTimeOffset>());
    }

    [Fact]
    public void GetThumbnailUrl_ProducesByteIdenticalUrls_AcrossBackToBackCalls()
    {
        const string blobId = "thumb-1";
        var capturedExpiries = new List<DateTimeOffset>();
        blobDataService
            .GetReadSas(blobId, Arg.Do<DateTimeOffset>(e => capturedExpiries.Add(e)))
            .Returns(callInfo => new Uri($"https://azurite/thumbnails/{blobId}?se={callInfo.ArgAt<DateTimeOffset>(1):O}"));

        var first = thumbnailUrlService.GetThumbnailUrl(blobId);
        var second = thumbnailUrlService.GetThumbnailUrl(blobId);

        Assert.Equal(first, second);
        Assert.Equal(2, capturedExpiries.Count);
        Assert.Equal(capturedExpiries[0], capturedExpiries[1]);
    }
}
