using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class UploadGroupVideoPage : ContentPage
{
    public UploadGroupVideoPage(UploadGroupVideoPageModel model)
    {
        this.BindingContext = model;
        InitializeComponent();
    }
}