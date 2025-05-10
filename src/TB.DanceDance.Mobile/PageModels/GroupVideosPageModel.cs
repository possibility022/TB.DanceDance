using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Models;

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

    [RelayCommand]
    private async Task Appearing()
    {
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
        await Shell.Current.GoToAsync("watchVideo", new Dictionary<string, object>()
        {
            { "videoBlobId", video.BlobId }
        });
    }

    [RelayCommand]
    private async Task NavigateToUploadGroupVideo()
    {
        await Shell.Current.GoToAsync("uploadVideoToGroup");
    }

    private async Task LoadData()
    {
        var loadedVideos = await videoProvider.GetGroupVideosAsync();
        Videos = loadedVideos;
    }
}