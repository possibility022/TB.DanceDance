using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TB.DanceDance.Mobile.PageModels;

namespace TB.DanceDance.Mobile.Pages;

public partial class UploadManagerPage : ContentPage
{
    public UploadManagerPage(UploadManagerPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}