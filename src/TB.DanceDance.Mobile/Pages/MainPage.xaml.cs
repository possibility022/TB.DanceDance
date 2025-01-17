using TB.DanceDance.Mobile.Models;
using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}