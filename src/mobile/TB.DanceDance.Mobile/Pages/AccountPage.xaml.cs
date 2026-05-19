namespace TB.DanceDance.Mobile.Pages;

public partial class AccountPage : ContentPage
{
    public AccountPage(PageModels.AccountPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
