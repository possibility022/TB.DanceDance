using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.PageModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IServiceProvider serviceProvider;
    private readonly TokenStorage primaryTokenStorage;
    private readonly TokenStorage secondaryTokenStorage;
    private Task? _checkHostTask = null;

    public MainPageViewModel(IServiceProvider serviceProvider, 
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)] TokenStorage primaryTokenStorage,
        [FromKeyedServices(TokenStorage.SecondaryStorageKey)] TokenStorage secondaryTokenStorage
        )
    {
        this.serviceProvider = serviceProvider;
        this.primaryTokenStorage = primaryTokenStorage;
        this.secondaryTokenStorage = secondaryTokenStorage;
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
    private async Task NavigateToMyVideos()
    {
        await Shell.Current.GoToAsync("//" + Routes.Private.MyVideos);
    }


    [RelayCommand]
    private async Task NavigateToGetAccess()
    {
        await Shell.Current.GoToAsync(Routes.GetAccess);
    }


    [RelayCommand]
    private async Task Logout()
    {
        primaryTokenStorage.ClearToken();
        secondaryTokenStorage.ClearToken();
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

            await serviceProvider.GetRequiredService<IDanceHttpApiClient>().GetUserAccesses();
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
            if (!IsLoggedIn && primaryTokenStorage.Token != null)
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
        if (primaryTokenStorage.Token is null)
            await primaryTokenStorage.LoadRefreshTokenFromStorage();

        if (_checkHostTask is not null)
            await _checkHostTask;

        if (primaryTokenStorage.Token?.AccessTokenExpiration is not null &&
            primaryTokenStorage.Token.AccessTokenExpiration > DateTimeOffset.Now.AddMinutes(-5))
        {
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false;
        }
    }
}