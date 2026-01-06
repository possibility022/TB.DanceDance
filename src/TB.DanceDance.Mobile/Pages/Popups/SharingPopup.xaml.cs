namespace TB.DanceDance.Mobile.Pages.Popups;

public partial class SharingPopup : ContentView
{
	public SharingPopup(SharingPopupViewModel sharingPopupViewModel)
	{
		InitializeComponent();
        BindingContext = sharingPopupViewModel;
	}
}