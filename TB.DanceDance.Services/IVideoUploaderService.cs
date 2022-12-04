using TB.DanceDance.Data.Blobs;

namespace TB.DanceDance.Services;

public interface IVideoUploaderService
{
    Uri GetSasUri();
}

public class VideoUploaderService : IVideoUploaderService
{
    private readonly IBlobDataService blobDataService;

    public VideoUploaderService(IBlobDataServiceFactory factory)
    {
        blobDataService = factory.GetBlobDataService(BlobContainer.VideosToConvert);
    }
    
    public Uri GetSasUri()
    {
        return blobDataService.CreateUploadSas();
    }
}