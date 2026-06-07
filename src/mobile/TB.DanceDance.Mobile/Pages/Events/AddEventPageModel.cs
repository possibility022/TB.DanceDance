using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.PageModels.Intents;

namespace TB.DanceDance.Mobile.PageModels;

public partial class AddEventPageModel : ObservableObject
{
    private readonly IDanceHttpApiClient apiClient;
    private readonly INavigationService navigationService;

    public AddEventPageModel(IDanceHttpApiClient apiClient, INavigationService navigationService)
    {
        this.apiClient = apiClient;
        this.navigationService = navigationService;
    }

    [ObservableProperty] private string _eventName = string.Empty;
    [ObservableProperty] private DateTime _eventDate = DateTime.Today;

    [RelayCommand]
    private async Task AddEvent()
    {
        var results = Validate();

        if (results is not null)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Ups", results, "Ok");
            return;
        }

        await apiClient.CreateEvent(EventName, EventDate);

        await navigationService.GoToAsync(
            Navigation.Relative().Pop().WithIntent(new RefreshEventsIntent()));
    }

    private string? Validate()
    {
        if (EventName.Length < 5)
            return "Nazwa wydarzenia jest za krótka :(.";
        return null;
    }
}
