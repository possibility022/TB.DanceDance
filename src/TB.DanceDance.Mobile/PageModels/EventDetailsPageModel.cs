using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventDetailsPageModel : ObservableObject, IQueryAttributable
{
    private readonly DanceHttpApiClient _apiClient;

    public EventDetailsPageModel(DanceHttpApiClient apiClient)
    {
        this._apiClient = apiClient;
    }
    
    [RelayCommand]
    private async Task NavigateToWatchVideo(VideoInformationResponse video)
    {
        await Shell.Current.GoToAsync("watchVideo", new Dictionary<string, object>()
        {
            { "videoBlobId", video.BlobId }
        });
    }

    [ObservableProperty] public Guid eventId;
    
    [ObservableProperty] public List<VideoInformationResponse> videos = [];
    
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
            var videosForEvent = await _apiClient.GetVideosForEvent(eventId);
            Videos = videosForEvent.ToList();
        }
    }
    
    [ObservableProperty] bool _isRefreshing;

}