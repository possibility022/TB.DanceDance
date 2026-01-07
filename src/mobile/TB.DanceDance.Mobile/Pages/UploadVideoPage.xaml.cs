using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class UploadVideoPage : ContentPage
{
    public UploadVideoPage(UploadVideoPageModel model)
    {
        this.BindingContext = model;
        InitializeComponent();
    }
}