using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
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
    
    [ObservableProperty] private List<Event> _userEvents = [new Event()
    {
        Id = Guid.Empty
        ,Name = "test",
        When = DateTime.Now
    }];
    private bool _isInitialized;

    [RelayCommand]
    private async Task Appearing()
    {
        Debug.WriteLine("Appearing Binded"); 
    }

    [RelayCommand]
    private async Task AddEvent()
    {
        await LoadData();
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
            //_errorHandler.HandleError(e);
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
    }
}