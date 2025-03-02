using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TB.DanceDance.Mobile.Pages;

public partial class EventsPage : ContentPage
{
    public EventsPage(EventsPageModel eventsPageModel)
    {
        BindingContext = eventsPageModel;
        InitializeComponent();
    }
}