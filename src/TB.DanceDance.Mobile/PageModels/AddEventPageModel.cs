using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class AddEventPageModel: ObservableObject
{
    private readonly IDanceHttpApiClient apiClient;

    public AddEventPageModel(IDanceHttpApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    [ObservableProperty] private string _eventName = string.Empty;
    [ObservableProperty] private DateTime _eventDate = DateTime.Today;
    
    [RelayCommand]
    private async Task AddEvent()
    {
        var results = Validate();

        if (results is not null)
        {
            await Shell.Current.CurrentPage.DisplayAlert("Ups", results, "Ok");
            return;
        }
        
        await apiClient.CreateEvent(EventName, EventDate);

        await Shell.Current.GoToAsync($"//{Routes.Events.EventsList}",
            new Dictionary<string, object>() { { "refreshEventList", true } });
    }

    private string? Validate()
    {
        if (EventName.Length < 5)
            return "Nazwa wydarzenia jest za krótka :(.";
        return null;
    }
    
}