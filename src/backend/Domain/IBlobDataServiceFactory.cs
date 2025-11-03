namespace Infrastructure.Data.BlobStorage;

public interface IBlobDataServiceFactory
{
    IBlobDataService GetBlobDataService(BlobContainer container);
}

public enum BlobContainer
{
    Videos,
    VideosToConvert
}