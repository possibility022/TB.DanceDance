using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class GetAccessPage : ContentPage
{
    public GetAccessPage(GetAccessPageModel viewModel)
    {
        InitializeComponent();
        this.BindingContext = viewModel;
    }
}