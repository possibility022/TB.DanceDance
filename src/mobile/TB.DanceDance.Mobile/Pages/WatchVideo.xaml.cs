using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class WatchVideo : ContentPage
{
    public WatchVideo(WatchVideoPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}