using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class BlobUploaderTests : IAsyncLifetime
{
    private readonly BlobUploader blobUploader;
    BlobContainerClient? client;

    public BlobUploaderTests()
    {
        blobUploader = new BlobUploader() { BufferSize = 100 };
    }

    [Fact]
    public async Task UploadWorks_ContentIsCorrect()
    {
        var blob = client.GetBlobClient(Guid.NewGuid().ToString());
        Uri uri = GenerateSas(blob);

        using MemoryStream ms = new();
        WriteDataBytes(ms);

        await blobUploader.UploadAsync(ms, uri, CancellationToken.None);

        ms.Position = 0;

        // Verify the blob was uploaded
        var download = await blob.DownloadAsync(TestContext.Current.CancellationToken);
        using MemoryStream downloadedMs = new();
        await download.Value.Content.CopyToAsync(downloadedMs, TestContext.Current.CancellationToken);

        // Check if the content is correct
        Assert.Equal(ms.ToArray(), downloadedMs.ToArray());
    }

    private Uri GenerateSas(BlobClient blob)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = client.Name,
            BlobName = blob.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Add | BlobSasPermissions.Read |
                                  BlobSasPermissions.Write);

        var uri = blob.GenerateSasUri(sasBuilder);
        return uri;
    }

    [Fact]
    public async Task Upload_CanContinueLater()
    {
        var blob = client.GetBlobClient(Guid.NewGuid().ToString());
        var uri = GenerateSas(blob);
        var cancellationTokenSource = new CancellationTokenSource();
        
        using MemoryStream ms = new();
        using MemoryStreamWrapper msWrapper = new(ms, 125, cancellationTokenSource);
        
        WriteDataBytes(ms);

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>  await blobUploader.UploadAsync(msWrapper, uri, cancellationTokenSource.Token));
            
        ms.Position = 0;
        
        await blobUploader.UploadAsync(ms, uri, TestContext.Current.CancellationToken);
        
        // Verify the blob was uploaded
        var download = await blob.DownloadAsync(TestContext.Current.CancellationToken);
        using MemoryStream downloadedMs = new();
        await download.Value.Content.CopyToAsync(downloadedMs, TestContext.Current.CancellationToken);

        // Check if the content is correct
        Assert.Equal(ms.ToArray(), downloadedMs.ToArray());
    }

    private static void WriteDataBytes(MemoryStream ms)
    {
        for (int i = 0; i < 350; i++)
        {
            ms.WriteByte(1);
            ms.WriteByte(5);
            ms.WriteByte(255);
            ms.WriteByte(251);
        }
        
        ms.Position = 0;
    }

    public async ValueTask InitializeAsync()
    {
        var container = await MobileDockerHelper.GetInitializedAzuriteContainer();
        var connectionString = container.GetConnectionString();
        this.client = new BlobContainerClient(connectionString, "videostoconvert");
        await this.client.CreateIfNotExistsAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}