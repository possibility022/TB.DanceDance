namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadVideoPage : ContentPage
{
    public UploadVideoPage(UploadVideoPageModel model)
    {
        this.BindingContext = model;
        InitializeComponent();
    }
}