using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class AddEventPage : ContentPage
{
    public AddEventPage(AddEventPageModel model)
    {
        this.BindingContext = model;
        InitializeComponent();
    }
}