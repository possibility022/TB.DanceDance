namespace TB.DanceDance.Mobile.Pages.Events;

public partial class AddEventPage : ContentPage
{
    public AddEventPage(AddEventPageModel model)
    {
        this.BindingContext = model;
        InitializeComponent();
    }
}