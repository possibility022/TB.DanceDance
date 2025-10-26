using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
#if ANDROID
        Application.Current.UserAppTheme = AppTheme.Light;
#endif
        BindingContext = viewModel;
        InitializeComponent();
    }
}