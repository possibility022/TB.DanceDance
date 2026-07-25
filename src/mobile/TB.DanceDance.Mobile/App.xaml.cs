using Nalu;
using TB.DanceDance.Mobile.Library.Data;

namespace TB.DanceDance.Mobile;

public partial class App : Application
{
    private readonly INavigationService _navigationService;
    private readonly IUploadStoreInitializer uploadStoreInitializer;
#if ANDROID
    private Window? _window;
#endif

    public App(
        INavigationService navigationService,
        IUploadStoreInitializer uploadStoreInitializer)
    {
        _navigationService = navigationService;
        this.uploadStoreInitializer = uploadStoreInitializer;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if ANDROID
        var window = _window ??= new Window(new AppShell(_navigationService));
#else
        var window = new Window(new AppShell(_navigationService));
#endif
        window.Dispatcher.Dispatch(() => _ = InitializeUploadStoreAsync());
        return window;
    }

    private async Task InitializeUploadStoreAsync()
    {
        try
        {
            await uploadStoreInitializer.EnsureInitializedAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Could not initialize upload storage.");
        }
    }
}
