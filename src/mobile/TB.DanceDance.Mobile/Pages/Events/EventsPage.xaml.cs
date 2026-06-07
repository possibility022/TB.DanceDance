namespace TB.DanceDance.Mobile.Pages.Events;

public partial class EventsPage : ContentPage
{
    public EventsPage(EventsPageModel eventsPageModel)
    {
        BindingContext = eventsPageModel;
        InitializeComponent();
    }
}