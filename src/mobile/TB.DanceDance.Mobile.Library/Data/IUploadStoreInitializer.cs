namespace TB.DanceDance.Mobile.Library.Data;

public interface IUploadStoreInitializer
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
}
