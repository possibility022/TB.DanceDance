using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Maui.Devices;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;
using TB.DanceDance.Tests.TestsFixture;

[assembly: AssemblyFixture(typeof(BlobStorageFixture))]

namespace TB.DanceDance.Mobile.Tests.IntegrationTests;

public class BlobUploaderTests : IAsyncLifetime
{
    private readonly BlobUploader blobUploader;
    BlobContainerClient client = null!;

    private readonly BlobStorageFixture fixture;
    
    public BlobUploaderTests(BlobStorageFixture fixture)
    {
        this.fixture = fixture;
        blobUploader = new BlobUploader(new NetworkAddressResolver(DevicePlatform.Unknown)) { BufferSize = 100 };
    }
    
    public async ValueTask InitializeAsync()
    {
        this.client = new BlobContainerClient(fixture.GetConnectionString(), "videostoconvert");
        await this.client.CreateIfNotExistsAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
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

    [Fact]
    public async Task Upload_ResumesPartialFinalBlock_AndCommitsIt()
    {
        var blob = client.GetBlobClient(Guid.NewGuid().ToString());
        var uri = GenerateSas(blob);
        var bytes = Enumerable.Range(0, 137).Select(value => (byte)value).ToArray();
        var blockId = Convert.ToBase64String(BitConverter.GetBytes(0));
        await using (var staged = new MemoryStream(bytes))
            await new BlockBlobClient(uri).StageBlockAsync(
                blockId,
                staged,
                cancellationToken: TestContext.Current.CancellationToken);

        await using var source = new MemoryStream(bytes);
        await blobUploader.UploadAsync(source, uri, TestContext.Current.CancellationToken);

        var downloaded = await blob.DownloadContentAsync(TestContext.Current.CancellationToken);
        Assert.Equal(bytes, downloaded.Value.Content.ToArray());
    }

    [Fact]
    public async Task Upload_AlreadyCommittedBlob_ReportsExactLengthWithoutReupload()
    {
        var blob = client.GetBlobClient(Guid.NewGuid().ToString());
        var uri = GenerateSas(blob);
        var bytes = Enumerable.Range(0, 137).Select(value => (byte)value).ToArray();
        await using (var first = new MemoryStream(bytes))
            await blobUploader.UploadAsync(first, uri, TestContext.Current.CancellationToken);

        long reported = -1;
        var progress = new InlineProgress<long>(value => reported = value);
        await using var retry = new MemoryStream(bytes);
        await blobUploader.UploadAsync(
            retry,
            uri,
            TestContext.Current.CancellationToken,
            progress);

        Assert.Equal(bytes.Length, reported);
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
}