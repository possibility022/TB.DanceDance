using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventDetailsPageModel : ObservableObject, IQueryAttributable
{
    private readonly VideoProvider videoProvider;

    public EventDetailsPageModel(VideoProvider videoProvider)
    {
        this.videoProvider = videoProvider;
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