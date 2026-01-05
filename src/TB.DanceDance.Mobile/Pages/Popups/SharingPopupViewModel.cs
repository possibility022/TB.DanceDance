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

    void OnCancel()
    {
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    void OnSave()
    {
    }

    bool CanSave() => string.IsNullOrWhiteSpace(name) is false;
}
