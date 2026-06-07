using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.PageModels.Intents;
using TB.DanceDance.Mobile.Pages.Popups;

namespace TB.DanceDance.Mobile.PageModels;

public partial class MyVideosPageModel : ObservableObject, IAppearingAware
{
    private const int PageSize = 20;

    [ObservableProperty] ObservableCollection<Video> videos = [];
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool canLoadMore;
    private readonly IDanceHttpApiClient apiClient;
    private readonly VideoProvider videoProvider;
    private readonly IPopupService popupService;
    private readonly INavigationService navigationService;
    private bool videosLoaded = false;
    private int currentPage = 0;

    public MyVideosPageModel(IDanceHttpApiClient apiClient, VideoProvider videoProvider, IPopupService popupService, INavigationService navigationService)
    {
        this.apiClient = apiClient;
        this.videoProvider = videoProvider;
        this.popupService = popupService;
        this.navigationService = navigationService;
    }

    [RelayCommand]
    private Task NavigateToWatchVideo(Video video)
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<WatchVideoPageModel>()
                .WithIntent(new WatchVideoIntent(video.BlobId)));

    public async ValueTask OnAppearingAsync()
    {
        if (!videosLoaded)
            await Refresh();
    }


    [RelayCommand]
    private async Task RenameVideo(Guid videoId)
    {
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
    private async Task ShareVideo(Guid videoId)
    {

        var video = Videos.First(r => r.Id == videoId);

        Dictionary<string, object> queryParams = new() {
            {SharingPopupViewModel.QueryAttribute_VideoId, videoId },
            {SharingPopupViewModel.QueryAttribute_VideoName, video.Name },
        };

        var res = await popupService.ShowPopupAsync<SharingPopupViewModel>(Shell.Current, options: PopupOptions.Empty, shellParameters: queryParams);
    }

    [RelayCommand]
    private Task NavigateToUpload()
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<UploadVideoPageModel>()
                .WithIntent(new UploadToPrivateIntent()));

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
            Serilog.Log.Error(e, "Error when refreshing event details.");
            //todo _errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (IsLoadingMore || !CanLoadMore)
            return;

        try
        {
            IsLoadingMore = true;

            var nextPage = currentPage + 1;
            var (items, totalCount) = await videoProvider.GetMyVideos(nextPage, PageSize);

            foreach (var video in items)
                Videos.Add(video);

            currentPage = nextPage;
            CanLoadMore = Videos.Count < totalCount;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when loading more videos.");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task LoadData()
    {
        var (items, totalCount) = await videoProvider.GetMyVideos(page: 1, PageSize);
        Videos = new ObservableCollection<Video>(items);
        currentPage = 1;
        CanLoadMore = Videos.Count < totalCount;
        videosLoaded = true;
    }
}
