using Domain.Services;

namespace Domain;

public interface IBlobDataServiceFactory
{
    IBlobDataService GetBlobDataService(BlobContainer container);
}

public enum BlobContainer
{
    Videos,
    VideosToConvert
}