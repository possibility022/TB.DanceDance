namespace TB.DanceDance.Mobile.Pages.Account;

public partial class AccountPage : ContentPage
{
    public AccountPage(AccountPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
