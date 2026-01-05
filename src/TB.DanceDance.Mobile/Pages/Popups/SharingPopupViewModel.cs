using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace TB.DanceDance.Mobile.Pages.Popups;

public partial class SharingPopupViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    readonly IPopupService popupService;

    public SharingPopupViewModel(IPopupService popupService)
    {
        this.popupService = popupService;
    }

    [RelayCommand]
    void Cancel()
    {
        Console.WriteLine("cancel");
    }

    [RelayCommand]
    void Share()
    {
        Console.WriteLine("Share");
    }
}
