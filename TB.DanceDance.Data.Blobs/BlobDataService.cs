using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TB.DanceDance.Data.Blobs;

public class BlobDataService : IBlobDataService
{
    private readonly string blobConnectionString;
    private BlobContainerClient container;

    public BlobDataService(string blobConnectionString, string containerName)
    {
        this.blobConnectionString =
            blobConnectionString ?? throw new ArgumentNullException(nameof(blobConnectionString));
        ConfigureBlob(containerName);
    }

    private void ConfigureBlob(string containerName)
    {
        container = new BlobContainerClient(blobConnectionString, containerName);
        container.CreateIfNotExists();
    }

    public Task<Stream> OpenStream(string blobName)
    {
        var client = container.GetBlobClient(blobName);
        return client.OpenReadAsync(new BlobOpenReadOptions(false));
    }

    public Task Upload(string blobId, Stream stream)
    {
        var client = container.GetBlobClient(blobId);
        return client.UploadAsync(stream);
    }

    public SharedBlob CreateUploadSas()
    {
        var blobClient = container.GetBlobClient(Guid.NewGuid().ToString());
        var sasBuilder = new BlobSasBuilder();

        // Be careful with SAS start time. If you set the start time for a SAS to the current time, failures might occur intermittently for the first few minutes.
        // This is due to different machines having slightly different current times (known as clock skew).
        // In general, set the start time to be at least 15 minutes in the past.
        // Or, don't set it at all, which will make it valid immediately in all cases.
        // The same generally applies to expiry time as well--remember that you may observe up to 15 minutes of clock skew in either direction on any request. For clients using a REST version prior to 2012-02-12,
        // the maximum duration for a SAS that does not reference a stored access policy is 1 hour. Any policies that specify a longer term than 1 hour will fail.
        //sasBuilder.StartsOn = DateTimeOffset.Now.AddMinutes(-25);
        sasBuilder.ExpiresOn = DateTimeOffset.Now.AddMinutes(60);
        sasBuilder.SetPermissions(BlobSasPermissions.Create);
        var sas = blobClient.GenerateSasUri(sasBuilder);
        return new SharedBlob()
        {
            BlobClient = blobClient,
            Sas = sas,
        };
    }
}

public class SharedBlob
{
    public BlobClient BlobClient { get; set; }
    public Uri Sas { get; set; }
}

public interface IBlobDataService
{
    Task<Stream> OpenStream(string blobName);
    Task Upload(string blobId, Stream stream);
    public SharedBlob CreateUploadSas();
}