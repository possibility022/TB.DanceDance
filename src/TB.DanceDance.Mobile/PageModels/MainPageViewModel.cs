using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Services.Auth;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly DanceHttpApiClient danceHttpApi;

    public MainPageViewModel(DanceHttpApiClient danceHttpApi)
    {
        this.danceHttpApi = danceHttpApi;
    }

    [ObservableProperty]
    private bool isLoggedIn;
    
    [ObservableProperty]
    private bool loginEnabled;
    
    [ObservableProperty]
    private bool loginInProgress;

    [RelayCommand]
    private async Task NavigateToGroups()
    {
        await Shell.Current.GoToAsync("//GroupVideosPage");
    }
    
    [RelayCommand]
    private async Task NavigateToEvents()
    {
        await Shell.Current.GoToAsync("//EventsPage");
    }
    
    [RelayCommand]
    private async Task Logout()
    {
        TokenStorage.ClearToken();
        LoginEnabled = true;
        IsLoggedIn = false;
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            LoginInProgress = true;
            LoginEnabled = false;
            await danceHttpApi.GetUserAccesses(); //todo create method to login
            await CheckLoginStatus();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error on login action on main page.");
        }
        finally
        {
            LoginEnabled = true;
            LoginInProgress = false;
        }
    }
    
    [RelayCommand]
    private async Task Appearing()
    {
        try
        {
            await CheckLoginStatus();
        }
        finally
        {
            LoginEnabled = true;
        }
    }

    private async Task CheckLoginStatus()
    {
        if (TokenStorage.Token is null)
            await TokenStorage.LoadRefreshTokenFromStorage();

        if (TokenStorage.Token?.AccessTokenExpiration is not null &&
            TokenStorage.Token.AccessTokenExpiration > DateTimeOffset.Now.AddMinutes(-5))
        {
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false;
        }
    }
}