using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Pages.Events;

namespace TB.DanceDance.Mobile.Pages;

public partial class MainPageViewModel : ObservableObject, IAppearingAware
{
    private readonly IServiceProvider serviceProvider;
    private readonly INavigationService navigationService;
    private readonly TokenStorage primaryTokenStorage;

    public MainPageViewModel(IServiceProvider serviceProvider,
        INavigationService navigationService,
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)] TokenStorage primaryTokenStorage)
    {
        this.serviceProvider = serviceProvider;
        this.navigationService = navigationService;
        this.primaryTokenStorage = primaryTokenStorage;
    }

    [ObservableProperty] private bool loginEnabled = true;
    [ObservableProperty] private bool loginInProgress;

    public async ValueTask OnAppearingAsync()
    {
        try
        {
            if (await IsAlreadyLoggedIn())
            {
                await GoToHomeTab();
                return;
            }

            if (primaryTokenStorage.Token?.RefreshToken is not null)
            {
                await Login();
            }
        }
        finally
        {
            LoginEnabled = true;
        }
    }

    [RelayCommand]
    private async Task Login()
    {
        try
        {
            LoginInProgress = true;
            LoginEnabled = false;

            await serviceProvider.GetRequiredService<IDanceHttpApiClient>().GetUserAccesses();

            if (await IsAlreadyLoggedIn())
            {
#if ANDROID
                UploadForegroundService.StartService();
#endif
                await GoToHomeTab();
            }
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

    private async Task<bool> IsAlreadyLoggedIn()
    {
        if (primaryTokenStorage.Token is null)
            await primaryTokenStorage.LoadRefreshTokenFromStorage();

        return primaryTokenStorage.Token?.AccessTokenExpiration is not null
               && primaryTokenStorage.Token.AccessTokenExpiration > DateTimeOffset.Now.AddMinutes(-5);
    }

    private Task GoToHomeTab()
        => navigationService.GoToAsync(Navigation.Absolute().Root<EventsPageModel>());
}
