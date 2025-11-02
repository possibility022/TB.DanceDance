using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Services.Auth;
using TB.DanceDance.Mobile.Services.DanceApi;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile.PageModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IServiceProvider serviceProvider;
    private readonly VideosDbContext dbContext;

    public MainPageViewModel(IServiceProvider serviceProvider, VideosDbContext dbContext)
    {
        this.serviceProvider = serviceProvider;
        this.dbContext = dbContext;
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
        await Shell.Current.GoToAsync("//" + Routes.Groups.AllVideos);
    }
    
    [RelayCommand]
    private async Task NavigateToEvents()
    {
        await Shell.Current.GoToAsync("//" + Routes.Events.EventsList);
    }

    [RelayCommand]
    private async Task NavigateToGetAccess()
    {
        await Shell.Current.GoToAsync(Routes.GetAccess);
    }
    
    
    [RelayCommand]
    private async Task Logout()
    {
        TokenStorage.ClearToken();
        LoginEnabled = true;
        IsLoggedIn = false;
#if ANDROID
        UploadForegroundService.StopService();
#endif
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            LoginInProgress = true;
            LoginEnabled = false;
            await HttpClientFactory.ValidatePrimaryHostIsAvailable();
            await serviceProvider.GetRequiredService<DanceHttpApiClient>().GetUserAccesses();
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
            if (!IsLoggedIn && TokenStorage.Token != null)
            {
                // We have a refresh token, just login automatically
                await Login();
            }
#if ANDROID
            //if (dbContext.VideosToUpload.Any(r => r.Uploaded == false))
                UploadForegroundService.StartService();
#endif
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