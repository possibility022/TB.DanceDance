using Domain;
using Infrastructure.Data.BlobStorage;
using System.Security.Cryptography;
using System.Text;
using TB.DanceDance.Tests.TestsFixture;

namespace TB.DanceDance.Tests.Infrastructure;

public class q
{
    [Fact]
    public void TestMethod()
    {
        using (SHA256 shA256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes("secretforlocalhttpclient");
            ;
            var h = Convert.ToBase64String(shA256.ComputeHash(bytes));
        }
        var x = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("secretforlocalhttpclient"));
    }
}

public class BlobDataServiceTests
{
    private readonly BlobStorageFixture blobStorageFixture;
    private readonly BlobDataServiceFactory factory;

    public BlobDataServiceTests(BlobStorageFixture blobStorageFixture)
    {
        this.blobStorageFixture = blobStorageFixture;
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
        await blobService.Upload(blobId, new MemoryStream(Array.Empty<byte>()));

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
