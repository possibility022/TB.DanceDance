using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models;

namespace TB.DanceDance.Mobile.PageModels;

public partial class GroupVideosPageModel : ObservableObject
{
    private readonly VideoProvider videoProvider;

    public GroupVideosPageModel(VideoProvider videoProvider)
    {
        this.videoProvider = videoProvider;
    }
    
    [ObservableProperty] private bool isRefreshing;
    
    [ObservableProperty] private List<Video> videos = [];

    private bool videoLoaded = false;

    [RelayCommand]
    private async Task Appearing()
    {
        if (!videoLoaded)
            await Refresh();
    }
    
    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            await LoadData();
        }
        catch (Exception e)
        {
            //_errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    
    [RelayCommand]
    private async Task NavigateToWatchVideo(Video video)
    {
        await Shell.Current.GoToAsync(Routes.Player, new Dictionary<string, object>()
        {
            { "videoBlobId", video.BlobId }
        });
    }

    [RelayCommand]
    private async Task NavigateToUploadGroupVideo()
    {
        await Shell.Current.GoToAsync(Routes.Upload.Uploader);
    }

    private async Task LoadData()
    {
        var loadedVideos = await videoProvider.GetGroupVideosAsync();
        Videos = loadedVideos;
        videoLoaded = true;
    }
}