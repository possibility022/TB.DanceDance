using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi
{
    public class BlobUploader
    {
        private readonly NetworkAddressResolver networkAddressResolver;

        public BlobUploader(NetworkAddressResolver networkAddressResolver)
        {
            this.networkAddressResolver = networkAddressResolver;
        }
        
        public int BufferSize { get; set; } = 1024 * 1024 * 4; // 4MB
        
        public event EventHandler<long>? UploadProgress;
        
        public async Task UploadAsync(
            Stream stream,
            Uri blobUri,
            CancellationToken cancellationToken,
            IProgress<long>? progress = null)
        {
            var address = networkAddressResolver.Resolve(blobUri);
            var blobClient = new BlockBlobClient(address);

            if (await IsAlreadyCommittedAsync(blobClient, stream.Length, cancellationToken))
            {
                ReportProgress(stream.Length, progress);
                return;
            }

            var existingBlocks = await GetUncommittedBlocksAsync(blobClient, cancellationToken);
            for (var index = 0; index < existingBlocks.Count; index++)
            {
                if (DecodeBlockId(existingBlocks[index].Name) != index)
                    throw new InvalidDataException("The staged blob block sequence is not contiguous.");
            }

            var blockList = existingBlocks.Select(block => block.Name).ToList();
            var resumeOffset = existingBlocks.Sum(block => block.SizeLong);

            byte[] buffer = new byte[BufferSize];
            var blockId = blockList.Count;
            
            if (resumeOffset > stream.Length)
                throw new InvalidDataException("The staged blob blocks are larger than the source file.");

            stream.Seek(resumeOffset, SeekOrigin.Begin);
            ReportProgress(resumeOffset, progress);

            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, BufferSize, cancellationToken)) > 0)
            {
                var blockIdBase64 = CreateBlockId(blockId);
                using (var memoryStream = new MemoryStream(buffer, 0, bytesRead))
                {
                    await blobClient.StageBlockAsync(blockIdBase64, memoryStream, null, cancellationToken);
                }
                blockList.Add(blockIdBase64);
                blockId++;
                resumeOffset += bytesRead;
                ReportProgress(Math.Min(resumeOffset, stream.Length), progress);
            }

            await blobClient.CommitBlockListAsync(blockList, cancellationToken: cancellationToken);
        }

        private static string CreateBlockId(int blockId) =>
            Convert.ToBase64String(BitConverter.GetBytes(blockId));

        private static async Task<bool> IsAlreadyCommittedAsync(
            BlockBlobClient blobClient,
            long expectedLength,
            CancellationToken token)
        {
            try
            {
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: token);
                return properties.Value.ContentLength == expectedLength;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }

        private static async Task<List<BlobBlock>> GetUncommittedBlocksAsync(
            BlockBlobClient blobClient,
            CancellationToken token)
        {
            try
            {
                var existingBlocks =
                    await blobClient.GetBlockListAsync(BlockListTypes.All, cancellationToken: token);
                return existingBlocks.Value.UncommittedBlocks
                    .OrderBy(block => DecodeBlockId(block.Name))
                    .ToList();
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

            return [];
        }

        private static int DecodeBlockId(string blockId)
        {
            var bytes = Convert.FromBase64String(blockId);
            if (bytes.Length != sizeof(int))
                throw new InvalidDataException("An uploaded block has an unsupported identifier.");
            return BitConverter.ToInt32(bytes);
        }

        private void ReportProgress(long value, IProgress<long>? progress)
        {
            progress?.Report(value);
            UploadProgress?.Invoke(this, value);
        }
    }
}