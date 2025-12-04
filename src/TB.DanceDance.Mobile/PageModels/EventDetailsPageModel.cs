using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventDetailsPageModel : ObservableObject, IQueryAttributable
{
    private readonly VideoProvider videoProvider;
    private readonly IDanceHttpApiClient apiClient;

    public EventDetailsPageModel(VideoProvider videoProvider, IDanceHttpApiClient apiClient)
    {
        this.videoProvider = videoProvider;
        this.apiClient = apiClient;
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
    private async Task NavigateToUploadToEvent()
    {
        await Shell.Current.GoToAsync(Routes.Upload.Uploader, new Dictionary<string, object>()
        {
            { "eventId", EventId }
        });
    }

    [ObservableProperty] Guid eventId;
    
    [ObservableProperty] List<Video> videos = [];
    [ObservableProperty] private bool isRefreshing;
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var weHaveIt = query.TryGetValue("eventId", out object? eventIdAsObject);
        if (weHaveIt && eventIdAsObject is Guid eventIdFromRoute)
        {
            EventId = eventIdFromRoute;
            Refresh();//todo fireandforgetsafeasync
        }
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