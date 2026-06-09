using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using System.Collections.ObjectModel;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Pages.Upload;
using TB.DanceDance.Mobile.Pages.WatchVideos;

namespace TB.DanceDance.Mobile.Pages.Groups;

public partial class GroupVideosPageModel : ObservableObject, IAppearingAware
{
    private const int PageSize = 20;

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
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool canLoadMore;

    [ObservableProperty] private ObservableCollection<Video> videos = [];

    private bool videoLoaded = false;
    private int currentPage = 0;

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

            var video = Enumerable.First<Video>(Videos, r => r.Id == videoId);
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
    private async Task DeleteVideo(Guid videoId)
    {
        try
        {
            var video = Videos.FirstOrDefault(r => r.Id == videoId);
            if (video == null)
                return;

            var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync("Usuń nagranie",
                $"Czy na pewno chcesz trwale usunąć „{video.Name}”? Usunięte zostaną również komentarze i linki do udostępnień.",
                "Usuń", "Anuluj");

            if (!confirmed)
                return;

            await apiClient.DeleteVideoAsync(videoId);
            Videos.Remove(video);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete video");
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

    [RelayCommand]
    private async Task LoadMore()
    {
        if (IsLoadingMore || !CanLoadMore)
            return;

        try
        {
            IsLoadingMore = true;

            var nextPage = currentPage + 1;
            var (items, totalCount) = await videoProvider.GetGroupVideosAsync(nextPage, PageSize);

            foreach (var video in items)
                Videos.Add(video);

            currentPage = nextPage;
            CanLoadMore = Videos.Count < totalCount;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when loading more group videos.");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task LoadData()
    {
        var (items, totalCount) = await videoProvider.GetGroupVideosAsync(page: 1, PageSize);
        Videos = new ObservableCollection<Video>(items);
        currentPage = 1;
        CanLoadMore = Videos.Count < totalCount;
        videoLoaded = true;
    }
}
