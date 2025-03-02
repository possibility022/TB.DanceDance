namespace TB.DanceDance.Mobile.Pages;

public partial class GroupVideosPage : ContentPage
{
    public GroupVideosPage(GroupVideosPageModel groupVideoPageModel)
    {
        BindingContext = groupVideoPageModel;
        InitializeComponent();
    }
}