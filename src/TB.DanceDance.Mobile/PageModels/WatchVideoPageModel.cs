using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using TB.DanceDance.Mobile.Services.DanceApi;

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
        using var stream = await apiClient.GetStream(videoBlobId);
        try
        {
            using var fileStream = File.OpenWrite(path);

            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();

            Media = MediaSource.FromFile(path);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            if (File.Exists(path))
                File.Delete(path);
        }
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