using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.PageModels.Intents;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventDetailsPageModel : ObservableObject,
    IEnteringAware<EventDetailsIntent>
{
    private const int PageSize = 20;

    private readonly VideoProvider videoProvider;
    private readonly IDanceHttpApiClient apiClient;
    private readonly INavigationService navigationService;
    private int currentPage = 0;

    public EventDetailsPageModel(VideoProvider videoProvider, IDanceHttpApiClient apiClient, INavigationService navigationService)
    {
        this.videoProvider = videoProvider;
        this.apiClient = apiClient;
        this.navigationService = navigationService;
    }

    [RelayCommand]
    private Task NavigateToWatchVideo(Video video)
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<WatchVideoPageModel>()
                .WithIntent(new WatchVideoIntent(video.BlobId)));


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
    private Task NavigateToUploadToEvent()
        => navigationService.GoToAsync(
            Navigation.Relative()
                .Push<UploadVideoPageModel>()
                .WithIntent(new UploadToEventIntent(EventId)));

    [ObservableProperty] Guid eventId;

    [ObservableProperty] ObservableCollection<Video> videos = [];
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool isLoadingMore;
    [ObservableProperty] private bool canLoadMore;

    public async ValueTask OnEnteringAsync(EventDetailsIntent intent)
    {
        EventId = intent.EventId;
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            await LoadData(EventId);
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
        if (IsLoadingMore || !CanLoadMore || EventId == Guid.Empty)
            return;

        try
        {
            IsLoadingMore = true;

            var nextPage = currentPage + 1;
            var (items, totalCount) = await videoProvider.GetEventVideos(EventId, nextPage, PageSize);

            foreach (var video in items)
                Videos.Add(video);

            currentPage = nextPage;
            CanLoadMore = Videos.Count < totalCount;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error when loading more event videos.");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private async Task LoadData(Guid eventId)
    {
        if (eventId != Guid.Empty)
        {
            var (items, totalCount) = await videoProvider.GetEventVideos(eventId, page: 1, PageSize);
            Videos = new ObservableCollection<Video>(items);
            currentPage = 1;
            CanLoadMore = Videos.Count < totalCount;
        }
    }
}
