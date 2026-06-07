namespace TB.DanceDance.Mobile.Pages.Groups;

public partial class GroupVideosPage : ContentPage
{
    public GroupVideosPage(GroupVideosPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}