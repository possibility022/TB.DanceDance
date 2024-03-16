namespace Application.Services;

public interface IBlobDataServiceFactory
{
    IBlobDataService GetBlobDataService(BlobContainer container);
}

public enum BlobContainer
{
    Videos,
    VideosToConvert
}