using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Nalu;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Pages.WatchVideos;

public partial class WatchVideoPageModel : ObservableObject,
    IEnteringAware<WatchVideoIntent>,
    ILeavingAware
{
    private readonly IDanceHttpApiClient apiClient;
    private CancellationTokenSource? loadingCancellation;

    public WatchVideoPageModel(IDanceHttpApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    private async Task LoadData(string videoBlobId, CancellationToken cancellationToken)
    {
#if DEBUG
        var path = Path.Combine(FileSystem.Current.CacheDirectory, videoBlobId + ".mp4");
        await using var stream = await apiClient.GetStream(videoBlobId, cancellationToken);
        try
        {
            using var fileStream = File.OpenWrite(path);

            await stream.CopyToAsync(fileStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);

            Media = MediaSource.FromFile(path);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error while saving vide into memory.");
            if (File.Exists(path))
                File.Delete(path);
        }

#else
        try
        {
            var (uri, token) = await apiClient.GetVideoUri(videoBlobId, cancellationToken);
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer " + token
            };
            var mediaSource = MediaSource.FromUri(uri, headers);
            if (mediaSource != null)
                Media = mediaSource;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error when setting video url.");
        }
#endif

    }

    [ObservableProperty] private MediaSource? media;

    public async ValueTask OnEnteringAsync(WatchVideoIntent intent)
    {
        loadingCancellation?.Cancel();
        loadingCancellation?.Dispose();
        loadingCancellation = new CancellationTokenSource();
        await LoadData(intent.VideoBlobId, loadingCancellation.Token);
    }

    public ValueTask OnLeavingAsync()
    {
        loadingCancellation?.Cancel();
        loadingCancellation?.Dispose();
        loadingCancellation = null;
        Media = null;
        return ValueTask.CompletedTask;
    }
}
