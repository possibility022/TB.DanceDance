using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}