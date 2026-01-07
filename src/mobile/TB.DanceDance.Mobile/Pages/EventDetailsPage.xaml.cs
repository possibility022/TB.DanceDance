using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class EventDetailsPage : ContentPage
{
    public EventDetailsPage(EventDetailsPageModel model)
    {
        BindingContext = model;
        InitializeComponent();
    }
}