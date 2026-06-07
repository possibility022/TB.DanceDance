namespace TB.DanceDance.Mobile.Pages.WatchVideos;

public partial class WatchVideo : ContentPage
{
    public WatchVideo(WatchVideoPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}