using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Nalu;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class WatchVideoPageModel : ObservableObject, IEnteringAware<WatchVideoIntent>
{
    private readonly IDanceHttpApiClient apiClient;

    public WatchVideoPageModel(IDanceHttpApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    private async Task LoadData(string videoBlobId)
    {
        var path = Path.Combine(FileSystem.Current.CacheDirectory, videoBlobId + ".mp4");

#if DEBUG
        await using var stream = await apiClient.GetStream(videoBlobId);
        try
        {
            using var fileStream = File.OpenWrite(path);

            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();

            Media = MediaSource.FromFile(path);
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
            var (uri, token) = apiClient.GetVideoUri(videoBlobId);
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer " + token
            };
            var mediaSource = MediaSource.FromUri(uri, headers);
            if (mediaSource != null)
                Media = mediaSource;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error when setting video url.");
        }
#endif

    }

    [ObservableProperty] private MediaSource media = null;

    public async ValueTask OnEnteringAsync(WatchVideoIntent intent)
    {
        await LoadData(intent.VideoBlobId);
    }
}
