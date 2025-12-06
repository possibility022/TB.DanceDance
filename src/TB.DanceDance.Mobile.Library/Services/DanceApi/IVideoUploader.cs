using TB.DanceDance.Mobile.Library.Data.Models.Storage;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public interface IVideoUploader
{
    Task Upload(VideosToUpload videoToUpload, CancellationToken token);
    Task UploadVideoToEvent(string fullPath, Guid value, CancellationToken none);
    Task UploadVideoToGroup(string fullPath, Guid id, CancellationToken none);
}
