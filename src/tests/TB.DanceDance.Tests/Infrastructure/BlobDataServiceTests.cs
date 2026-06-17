using Domain;
using Infrastructure.Data.BlobStorage;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Infrastructure;


public class BlobDataServiceTests
{
    private readonly BlobDataServiceFactory factory;

    public BlobDataServiceTests(BlobStorageFixture blobStorageFixture)
    {
        this.factory = new BlobDataServiceFactory(blobStorageFixture.GetConnectionString());
    }

    [Fact]
    public async Task GetBlobSizeAsync_ReturnsCorrectSize_ForExistingBlob()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Videos);
        var blobId = Guid.NewGuid().ToString();
        var testData = new byte[4096]; // 4KB
        Array.Fill(testData, (byte)42);
        await blobService.Upload(blobId, new MemoryStream(testData));

        // Act
        var size = await blobService.GetBlobSizeAsync(blobId);

        // Assert
        Assert.Equal(4096, size);
    }

    [Fact]
    public async Task GetBlobSizeAsync_ReturnsZero_ForEmptyBlob()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Videos);
        var blobId = Guid.NewGuid().ToString();
        await blobService.Upload(blobId, new MemoryStream([]));

        // Act
        var size = await blobService.GetBlobSizeAsync(blobId);

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public async Task GetBlobSizeAsync_Throws_ForNonExistentBlob()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Videos);
        var nonExistentBlobId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
        {
            await blobService.GetBlobSizeAsync(nonExistentBlobId);
        });
    }

    [Fact]
    public async Task GetBlobSizeAsync_ReturnsCorrectSize_ForLargeBlob()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.VideosToConvert);
        var blobId = Guid.NewGuid().ToString();
        var testData = new byte[1024 * 1024]; // 1MB
        Array.Fill(testData, (byte)255);
        await blobService.Upload(blobId, new MemoryStream(testData));

        // Act
        var size = await blobService.GetBlobSizeAsync(blobId);

        // Assert
        Assert.Equal(1024 * 1024, size);
    }

    [Fact]
    public void GetReadSas_WithExplicitExpiry_ProducesByteIdenticalUrls_ForSameExpiry()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Thumbnails);
        var blobId = Guid.NewGuid().ToString();
        var expiresOn = new DateTimeOffset(2026, 6, 7, 10, 30, 0, TimeSpan.Zero);

        // Act
        var first = blobService.GetReadSas(blobId, expiresOn);
        var second = blobService.GetReadSas(blobId, expiresOn);

        // Assert
        Assert.Equal(first.ToString(), second.ToString());
        Assert.Equal(first.Query, second.Query);
    }

    [Fact]
    public void GetReadSas_WithExplicitExpiry_ProducesDifferentUrls_ForDifferentExpiry()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Thumbnails);
        var blobId = Guid.NewGuid().ToString();

        // Act
        var first = blobService.GetReadSas(blobId, new DateTimeOffset(2026, 6, 7, 10, 30, 0, TimeSpan.Zero));
        var second = blobService.GetReadSas(blobId, new DateTimeOffset(2026, 6, 7, 11, 0, 0, TimeSpan.Zero));

        // Assert
        Assert.NotEqual(first.ToString(), second.ToString());
    }

    [Fact]
    public void GetReadSas_WithoutExplicitExpiry_StillProducesAReadableSasUri()
    {
        // Arrange
        var blobService = factory.GetBlobDataService(BlobContainer.Thumbnails);
        var blobId = Guid.NewGuid().ToString();

        // Act
        var sas = blobService.GetReadSas(blobId);

        // Assert
        Assert.Contains("sig=", sas.Query);
    }

    [Fact]
    public async Task GetBlobSizeAsync_WorksAcrossDifferentContainers()
    {
        // Arrange
        var videosService = factory.GetBlobDataService(BlobContainer.Videos);
        var toConvertService = factory.GetBlobDataService(BlobContainer.VideosToConvert);

        var blobId1 = Guid.NewGuid().ToString();
        var blobId2 = Guid.NewGuid().ToString();

        var data1 = new byte[100];
        var data2 = new byte[200];

        await videosService.Upload(blobId1, new MemoryStream(data1));
        await toConvertService.Upload(blobId2, new MemoryStream(data2));

        // Act
        var size1 = await videosService.GetBlobSizeAsync(blobId1);
        var size2 = await toConvertService.GetBlobSizeAsync(blobId2);

        // Assert
        Assert.Equal(100, size1);
        Assert.Equal(200, size2);
    }
}
