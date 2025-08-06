using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Data.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventsPageModel : ObservableObject, IQueryAttributable
{
    private readonly DanceHttpApiClient _apiClient;

    public EventsPageModel(DanceHttpApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    [ObservableProperty] bool _isRefreshing;
    
    [ObservableProperty] private List<Event> _userEvents = [];

    private bool eventsLoaded = false;

    [RelayCommand]
    private async Task Appearing()
    {
        if (!eventsLoaded)
            await Refresh();
    }

    [RelayCommand]
    private async Task NavigateToEventDetails(Event @event)
    {
        await Shell.Current.GoToAsync(Routes.Events.EventDetails, new Dictionary<string, object>()
        {
            { "eventId", @event.Id }
        });
    }

    [RelayCommand]
    private async Task NavigateToAddEvent()
    {
        await Shell.Current.GoToAsync(Routes.Events.Add);
    }
    
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var weHaveIt = query.TryGetValue("refreshEventList", out object? refreshListRequired);
        if (weHaveIt && refreshListRequired is bool refreshListRequiredAsBool)
        {
            if (refreshListRequiredAsBool)
                Refresh();//todo fireandforgetsafeasync
        }
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
            //todo _errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadData()
    {
        var accesses = await _apiClient.GetUserAccesses();
        if (accesses != null)
            UserEvents = accesses.Assigned.Events.Select(Event.MapFromApiEvent).ToList();;
        
        eventsLoaded = true;
    }
}