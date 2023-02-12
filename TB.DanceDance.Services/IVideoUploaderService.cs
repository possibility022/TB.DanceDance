using TB.DanceDance.Data.Blobs;

namespace TB.DanceDance.Services;

public interface IVideoUploaderService
{
    SharedBlob GetSasUri();
}

public class VideoUploaderService : IVideoUploaderService
{
    private readonly IBlobDataService blobDataService;

    public VideoUploaderService(IBlobDataServiceFactory factory)
    {
        blobDataService = factory.GetBlobDataService(BlobContainer.VideosToConvert);
    }
    
    public SharedBlob GetSasUri()
    {
        return blobDataService.CreateUploadSas();
    }
}