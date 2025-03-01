using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class BlobUploaderTests : IAsyncLifetime
{
    private readonly BlobUploader blobUploader;
    BlobContainerClient client;

    public BlobUploaderTests()
    {
        blobUploader = new BlobUploader() { BufferSize = 100 };
    }

    [Fact]
    public async Task UploadWorks_ContentIsCorrect()
    {
        var blob = client.GetBlobClient(Guid.NewGuid().ToString());
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

        using MemoryStream ms = new();
        for (int i = 0; i < 350; i++)
        {
            ms.WriteByte(1);
        }

        ms.Position = 0;

        await blobUploader.UploadFileAsync(ms, uri, CancellationToken.None);

        ms.Position = 0;

        // Verify the blob was uploaded
        var download = await blob.DownloadAsync();
        using MemoryStream downloadedMs = new();
        await download.Value.Content.CopyToAsync(downloadedMs);

        // Check if the content is correct
        Assert.Equal(ms.ToArray(), downloadedMs.ToArray());
    }

    public async Task InitializeAsync()
    {
        var container = await DockerHelper.GetInitializedAzuriteContainer();
        var connectionString = container.GetConnectionString();

        var serviceClient = new BlobServiceClient(connectionString);
        await serviceClient.CreateBlobContainerAsync("videostoconvert");
        this.client = new BlobContainerClient(connectionString, "videostoconvert");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}