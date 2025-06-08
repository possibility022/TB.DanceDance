using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile.Services.DanceApi
{
    public class BlobUploader
    {
        public int BufferSize { get; set; } = 1024 * 1024 * 4; // 4MB
        
        public event EventHandler<int>? UploadProgress;

        public async Task UploadFileAsync(Stream stream, Uri blobUri, CancellationToken cancellationToken)
        {
            var address = NetworkAddressResolver.Resolve(blobUri);
            var blobClient = new BlockBlobClient(address);
            var blockList = new List<string>();

            byte[] buffer = new byte[BufferSize];
            int bytesRead;
            int blockId = 0;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
            {
                var blockIdBase64 = Convert.ToBase64String(BitConverter.GetBytes(blockId));
                using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                {
                    await blobClient.StageBlockAsync(blockIdBase64, memoryStream, null, cancellationToken);
                }

                blockList.Add(blockIdBase64);
                blockId++;
                OnUploadProgress(blockId * BufferSize);
                await Task.Delay(1000);
            }

            await blobClient.CommitBlockListAsync(blockList, cancellationToken: cancellationToken);
        }

        public async Task ResumeUploadAsync(Stream stream, Uri blobUri, CancellationToken cancellationToken)
        {
            var address = NetworkAddressResolver.Resolve(blobUri);
            var blobClient = new BlockBlobClient(address);

            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (exists != true)
            {
                await UploadFileAsync(stream, blobUri, cancellationToken);
                return;
            }
                
            var existingBlocks =
                await blobClient.GetBlockListAsync(BlockListTypes.All, cancellationToken: cancellationToken);
            var blockList = existingBlocks.Value.UncommittedBlocks.Select(b => b.Name).ToList();

            byte[] buffer = new byte[BufferSize];
            
            int blockId = blockList.Count;

            stream.Seek(blockId * BufferSize, SeekOrigin.Begin);

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

        protected virtual void OnUploadProgress(int e)
        {
            UploadProgress?.Invoke(this, e);
        }
    }
}