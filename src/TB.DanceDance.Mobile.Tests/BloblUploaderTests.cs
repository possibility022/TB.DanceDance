using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.Tests;

public class BloblUploaderTests
{
    private readonly BlobUploader blobUploader;

    private const string DefaultAzureStorageConnectionString = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

    public BloblUploaderTests()
    {
        blobUploader = new BlobUploader()
        {
            BufferSize = 100
        };
    }

    [Fact]
    public async Task UploadWorks_ContentIsCorrect()
    {
        BlobContainerClient client = new(DefaultAzureStorageConnectionString, "videostoconvert");

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
}
