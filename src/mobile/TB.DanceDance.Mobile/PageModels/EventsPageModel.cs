using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.PageModels.Intents;

namespace TB.DanceDance.Mobile.PageModels;

public partial class EventsPageModel : ObservableObject,
    IAppearingAware,
    IAppearingAware<RefreshEventsIntent>
{
    private readonly IDanceHttpApiClient _apiClient;
    private readonly INavigationService _navigationService;
    private bool _eventsLoaded;

    public EventsPageModel(IDanceHttpApiClient apiClient, INavigationService navigationService)
    {
        _apiClient = apiClient;
        _navigationService = navigationService;
    }

    [ObservableProperty] private bool _isRefreshing;

    [ObservableProperty] private List<Event> _userEvents = [];

    public async ValueTask OnAppearingAsync()
    {
        if (!_eventsLoaded)
            await Refresh();
    }

    public async ValueTask OnAppearingAsync(RefreshEventsIntent intent)
        => await Refresh();

    [RelayCommand]
    private Task NavigateToEventDetails(Event @event)
        => _navigationService.GoToAsync(
            Navigation.Relative()
                .Push<EventDetailsPageModel>()
                .WithIntent(new EventDetailsIntent(@event.Id)));

    [RelayCommand]
    private Task NavigateToAddEvent()
        => _navigationService.GoToAsync(
            Navigation.Relative().Push<AddEventPageModel>());

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
            UserEvents = accesses.Assigned.Events.Select(Event.MapFromApiEvent).ToList();

        _eventsLoaded = true;
    }
}
