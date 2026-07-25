using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Library.Services.DanceApi;

public class VideoUploader : IVideoUploader
{
    private readonly BlobUploader uploader;

    public VideoUploader(NetworkAddressResolver networkAddressResolver)
    {
        uploader = new BlobUploader(networkAddressResolver);
    }

    public async Task Upload(
        VideosToUpload videoToUpload,
        IProgress<long>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(videoToUpload);

        if (videoToUpload.State == UploadJobState.Completed || videoToUpload.Uploaded)
            return;

        if (string.IsNullOrWhiteSpace(videoToUpload.Sas))
            throw new InvalidOperationException("The upload job does not have a SAS URL.");

        if (videoToUpload.SasExpireAt <= DateTime.UtcNow.AddMinutes(5))
            throw new InvalidOperationException("The upload SAS URL has expired.");

        var file = new FileInfo(videoToUpload.FullFileName);
        if (!file.Exists)
            throw new FileNotFoundException("The staged video file is missing.", file.FullName);

        await using var fileStream = file.OpenRead();
        await uploader.UploadAsync(
            fileStream,
            new Uri(videoToUpload.Sas),
            cancellationToken,
            progress);
    }
}