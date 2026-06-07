using Nalu;
using TB.DanceDance.Mobile.Pages;

namespace TB.DanceDance.Mobile;

public partial class AppShell : NaluShell
{
    public AppShell(INavigationService navigationService)
        : base(navigationService, typeof(MainPage))
    {
        InitializeComponent();
    }
}
