using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class GroupVideosPageModel : ObservableObject
{
    private readonly DanceHttpApiClient _apiClient;

    public GroupVideosPageModel(DanceHttpApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    [ObservableProperty] bool _isRefreshing;
    
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

    private async Task LoadData()
    {
        var response = await _apiClient.GetVideosFromGroups();
        Videos = Video.MapFromApiResponse(response);
    }
}