namespace TB.DanceDance.Mobile.Pages;

public partial class MyVideosPage : ContentPage
{
	public MyVideosPage(MyVideosPageModel model)
	{
        BindingContext = model;
        InitializeComponent();
	}
}