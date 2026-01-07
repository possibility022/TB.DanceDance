using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class GroupVideosPage : ContentPage
{
    public GroupVideosPage(GroupVideosPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}