namespace TB.DanceDance.Mobile.Pages.Events;

public partial class EventDetailsPage : ContentPage
{
    public EventDetailsPage(EventDetailsPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}