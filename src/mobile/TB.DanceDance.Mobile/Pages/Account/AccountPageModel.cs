using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using System.Text;
using System.Text.Json;
using TB.DanceDance.Mobile.Library.Services.Auth;
using TB.DanceDance.Mobile.Pages.Access;

namespace TB.DanceDance.Mobile.Pages.Account;

public partial class AccountPageModel : ObservableObject, IAppearingAware
{
    private readonly INavigationService navigationService;
    private readonly TokenStorage primaryTokenStorage;

    public AccountPageModel(
        INavigationService navigationService,
        [FromKeyedServices(TokenStorage.PrimaryStorageKey)] TokenStorage primaryTokenStorage)
    {
        this.navigationService = navigationService;
        this.primaryTokenStorage = primaryTokenStorage;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUserDisplay))]
    private string userDisplay = string.Empty;

    public bool HasUserDisplay => !string.IsNullOrWhiteSpace(UserDisplay);

    public ValueTask OnAppearingAsync()
    {
        UserDisplay = ExtractUserDisplay(primaryTokenStorage.Token?.IdentityToken);
        return default;
    }

    [RelayCommand]
    private Task NavigateToGetAccess()
        => navigationService.GoToAsync(Navigation.Relative().Push<GetAccessPageModel>());

    [RelayCommand]
    private async Task Logout()
    {
        primaryTokenStorage.ClearToken();
#if ANDROID
        UploadForegroundService.StopService();
#endif
        await navigationService.GoToAsync(Navigation.Absolute().Root<MainPageViewModel>());
    }

    private static string ExtractUserDisplay(string? identityToken)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
            return string.Empty;

        var parts = identityToken.Split('.');
        if (parts.Length < 2)
            return string.Empty;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String)
                return email.GetString() ?? string.Empty;
            if (root.TryGetProperty("preferred_username", out var u) && u.ValueKind == JsonValueKind.String)
                return u.GetString() ?? string.Empty;
            if (root.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                return n.GetString() ?? string.Empty;
        }
        catch
        {
        }

        return string.Empty;
    }
}
