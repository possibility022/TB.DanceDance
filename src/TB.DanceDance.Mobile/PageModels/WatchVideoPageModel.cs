using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class WatchVideoPageModel : ObservableObject, IQueryAttributable
{
    private readonly DanceHttpApiClient apiClient;

    public WatchVideoPageModel(DanceHttpApiClient apiClient)
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
            var uri = apiClient.GetVideoUri(videoBlobId);
            var mediaSource = MediaSource.FromUri(uri.ToString());
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var weHaveIt = query.TryGetValue("videoBlobId", out var videoIdAsObject);
        if (weHaveIt && videoIdAsObject is string routeVideoId)
        {
            LoadData(routeVideoId); //todo fire and forget async safe
        }
    }
}