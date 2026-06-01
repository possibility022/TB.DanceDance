namespace TB.DanceDance.Utilities.Infrastructure;

public interface IBlobDataServiceFactory
{
    IBlobDataService GetBlobDataService(BlobContainer container);
}

public enum BlobContainer
{
    Videos,
    VideosToConvert
}