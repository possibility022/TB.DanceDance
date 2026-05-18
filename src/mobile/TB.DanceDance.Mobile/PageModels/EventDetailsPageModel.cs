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
    private readonly VideoProvider videoProvider;
    private readonly IDanceHttpApiClient apiClient;
    private readonly INavigationService navigationService;

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

    [ObservableProperty] List<Video> videos = [];
    [ObservableProperty] private bool isRefreshing;

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

    private async Task LoadData(Guid eventId)
    {
        if (eventId != Guid.Empty)
        {
            var providedVideos = await videoProvider.GetEventVideos(eventId);
            Videos = providedVideos;
        }
    }
}
