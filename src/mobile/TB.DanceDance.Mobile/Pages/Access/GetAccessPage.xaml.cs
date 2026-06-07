namespace TB.DanceDance.Mobile.Pages.Access;

public partial class GetAccessPage : ContentPage
{
    public GetAccessPage(GetAccessPageModel viewModel)
    {
        InitializeComponent();
        this.BindingContext = viewModel;
    }
}