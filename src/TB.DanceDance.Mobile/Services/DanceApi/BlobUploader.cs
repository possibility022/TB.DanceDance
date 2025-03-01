using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace TB.DanceDance.Mobile.Services.DanceApi
{
    public class BlobUploader
    {
        public int BufferSize { get; set; } = 1024 * 1024 * 4; // 4MB


        public async Task UploadFileAsync(Stream stream, Uri blobUri, CancellationToken cancellationToken)
        {
            var blobClient = new BlockBlobClient(blobUri);
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
            }

            await blobClient.CommitBlockListAsync(blockList, cancellationToken: cancellationToken);
        }

        public async Task ResumeUploadAsync(string filePath, Uri blobUri, CancellationToken cancellationToken)
        {
            var blobClient = new BlockBlobClient(blobUri);

            var existingBlocks =
                await blobClient.GetBlockListAsync(BlockListTypes.All, cancellationToken: cancellationToken);
            var blockList = existingBlocks.Value.UncommittedBlocks.Select(b => b.Name).ToList();

            byte[] buffer = new byte[BufferSize];
            int blockId = blockList.Count;

            await using (var fileStream = File.OpenRead(filePath))
            {
                fileStream.Seek(blockId * BufferSize, SeekOrigin.Begin);

                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
                {
                    var blockIdBase64 = Convert.ToBase64String(BitConverter.GetBytes(blockId));
                    using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                    {
                        await blobClient.StageBlockAsync(blockIdBase64, memoryStream, null, cancellationToken);
                    }

                    blockList.Add(blockIdBase64);
                    blockId++;
                }
            }

            await blobClient.CommitBlockListAsync(blockList, cancellationToken: cancellationToken);
        }
    }
}