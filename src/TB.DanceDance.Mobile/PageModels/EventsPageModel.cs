using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventsPageModel : ObservableObject
{
    private readonly DanceHttpApiClient _apiClient;

    public EventsPageModel(DanceHttpApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    [ObservableProperty] bool _isRefreshing;
    
    [ObservableProperty] private List<Event> _userEvents = [];

    
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

    private async Task LoadData()
    {
        var acesses = await _apiClient.GetUserAccesses();
        if (acesses != null)
            UserEvents = Event.MapFromApiEvent(acesses);
    }
}