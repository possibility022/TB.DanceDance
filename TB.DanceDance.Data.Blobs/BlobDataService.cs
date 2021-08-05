using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TB.DanceDance.Data.Db;

namespace TB.DanceDance.Data.Blobs
{
    public class BlobDataService : IBlobDataService
    {
        private readonly string blobConnectionString;
        private BlobContainerClient container;

        public BlobDataService(string blobConnectionString)
        {
            this.blobConnectionString = blobConnectionString;
            ConfigureBlob();
        }

        private void ConfigureBlob()
        {
            container = new BlobContainerClient(blobConnectionString, Constants.Infrastructure.VideoBlobContainerName);
            container.CreateIfNotExists();
        }

        public Task<Stream> OpenStream(string blobName)
        {
            var client = container.GetBlobClient(blobName);
            return client.OpenReadAsync(new BlobOpenReadOptions(false));
        }

    }

    public interface IBlobDataService
    {
        Task<Stream> OpenStream(string blobName);
    }
}
