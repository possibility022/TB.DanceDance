namespace TB.DanceDance.Mobile.Pages.WatchVideos;

public partial class WatchVideo : ContentPage
{
    public WatchVideo(WatchVideoPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Player.ShouldKeepScreenOn = true;
    }

    protected override void OnDisappearing()
    {
        Player.Stop();
        Player.ShouldKeepScreenOn = false;
        base.OnDisappearing();
    }
}