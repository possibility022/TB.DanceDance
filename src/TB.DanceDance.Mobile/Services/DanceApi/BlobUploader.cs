using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile.Services.DanceApi
{
    public class BlobUploader
    {
        public int BufferSize { get; set; } = 1024 * 1024 * 4; // 4MB
        
        public event EventHandler<int>? UploadProgress;
        
        public async Task UploadAsync(Stream stream, Uri blobUri, CancellationToken cancellationToken)
        {
            var address = NetworkAddressResolver.Resolve(blobUri);
            var blobClient = new BlockBlobClient(address);
            
            var blockList = await CheckIfSomethingUploaded(blobClient, cancellationToken);

            byte[] buffer = new byte[BufferSize];
            var blockId = blockList.Count;
            
            if (blockId > 0)
            {
                stream.Seek(blockId * BufferSize, SeekOrigin.Begin);
            }

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
            {
                var blockIdBase64 = Convert.ToBase64String(BitConverter.GetBytes(blockId));
                using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                {
                    // interesting, why I can do it using arrays and I have to initialize memory stream
                    await blobClient.StageBlockAsync(blockIdBase64, memoryStream, null, cancellationToken);
                }
                blockList.Add(blockIdBase64);
                blockId++;
                OnUploadProgress(blockId * BufferSize);
            }

            await blobClient.CommitBlockListAsync(blockList, cancellationToken: cancellationToken);
        }

        private static async Task<List<string>> CheckIfSomethingUploaded(BlockBlobClient blobClient, CancellationToken token)
        {
            try
            {
                var existingBlocks =
                    await blobClient.GetBlockListAsync(BlockListTypes.All, cancellationToken: token);
                var blockList = existingBlocks.Value.UncommittedBlocks.Select(b => b.Name).ToList();
                return blockList;
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == 404)
                {
                    // nothing to do   
                }
                else
                {
                    throw;
                }
            }

            return new List<string>();
        }

        protected void OnUploadProgress(int e)
        {
            UploadProgress?.Invoke(this, e);
        }
    }
}