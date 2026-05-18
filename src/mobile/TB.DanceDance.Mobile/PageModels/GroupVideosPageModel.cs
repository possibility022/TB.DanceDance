using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.PageModels.Intents;

namespace TB.DanceDance.Mobile.PageModels;

public partial class GroupVideosPageModel : ObservableObject, IAppearingAware
{
    private readonly VideoProvider videoProvider;
    private readonly IDanceHttpApiClient apiClient;
    private readonly INavigationService navigationService;

    public GroupVideosPageModel(VideoProvider videoProvider, IDanceHttpApiClient apiClient, INavigationService navigationService)
    {
        this.videoProvider = videoProvider;
        this.apiClient = apiClient;
        this.navigationService = navigationService;
    }

    [ObservableProperty] private bool isRefreshing;

    [ObservableProperty] private List<Video> videos = [];

    private bool videoLoaded = false;

    public async ValueTask OnAppearingAsync()
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
        catch (Exception)
        {
            //_errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }


    [RelayCommand]
    private async Task RenameVideo(Guid videoId)
    {
        // TODO this code is duplicated in event details page model.
        // Refactor it in future.
        try
        {
            var newName = await Shell.Current.CurrentPage.DisplayPromptAsync("Zmień nazwę",
                "Podaj nową nazwę dla nagrania.", "Ok",
                "Anuluj");

            if (newName == null)
                return;

            var video = Videos.First(r => r.Id == videoId);
            if (video.Name == newName)
                return;

            if (newName.Length is < 5 or > 50)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Zła nazwa", "Nazwa musi mieć od 5 do 50 znaków.", "Ok");
                return;
            }

            await apiClient.RenameVideoAsync(videoId, newName);
            video.Name = newName;

            await Refresh();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not rename video");
        }
    }

    [RelayCommand]
    private Task NavigateToWatchVideo(Video video)
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<WatchVideoPageModel>()
                .WithIntent(new WatchVideoIntent(video.BlobId)));

    [RelayCommand]
    private Task NavigateToUploadGroupVideo()
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<UploadVideoPageModel>()
                .WithIntent(new UploadToGroupIntent()));

    private async Task LoadData()
    {
        var loadedVideos = await videoProvider.GetGroupVideosAsync();
        Videos = loadedVideos;
        videoLoaded = true;
    }
}
