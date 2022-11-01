using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TB.DanceDance.Data.Blobs
{
    public class BlobDataService : IBlobDataService
    {
        private readonly string blobConnectionString;
        private BlobContainerClient container;

        public BlobDataService(string blobConnectionString, string containerName)
        {
            this.blobConnectionString = blobConnectionString ?? throw new ArgumentNullException(nameof(blobConnectionString));
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

    }

    public interface IBlobDataService
    {
        Task<Stream> OpenStream(string blobName);
        Task Upload(string blobId, Stream stream);
    }
}
