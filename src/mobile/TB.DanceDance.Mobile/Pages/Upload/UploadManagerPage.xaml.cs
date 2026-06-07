namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadManagerPage : ContentPage
{
    public UploadManagerPage(UploadManagerPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}