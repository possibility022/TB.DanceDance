using Nalu;

namespace TB.DanceDance.Mobile;

public partial class App : Application
{
    private readonly INavigationService _navigationService;
#if ANDROID
    private Window? _window;
#endif

    public App(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if ANDROID
        return _window ??= new Window(new AppShell(_navigationService));
#else
        return new Window(new AppShell(_navigationService));
#endif
    }
}
