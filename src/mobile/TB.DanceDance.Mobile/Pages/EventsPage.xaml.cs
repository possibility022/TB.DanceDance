using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class EventsPage : ContentPage
{
    public EventsPage(EventsPageModel eventsPageModel)
    {
        BindingContext = eventsPageModel;
        InitializeComponent();
    }
}