using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.API.Contracts.Responses;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
namespace TB.DanceDance.Mobile.Pages.Popups;

public partial class SharingPopupViewModel : ObservableObject, IQueryAttributable
{
    readonly IPopupService popupService;
    private readonly IDanceHttpApiClient danceHttpApiClient;

    public const string QueryAttribute_VideoId = "qVideoId";
    public const string QueryAttribute_VideoName = "qVideoName";

    private Guid videoId;

    private SharedLinkResponse? response;

    [ObservableProperty]
    private string videoName = string.Empty;

    [ObservableProperty]
    private bool showLoader = false;

    public bool CanExecute() {  return !ShowLoader; }

    [ObservableProperty]
    private string primaryButtonText = "Udostępnij";

    public SharingPopupViewModel(IPopupService popupService, IDanceHttpApiClient danceHttpApiClient)
    {
        this.popupService = popupService;
        this.danceHttpApiClient = danceHttpApiClient;
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    async Task Cancel()
    {
        if (response is not null)
        {
            try
            {
                await danceHttpApiClient.RevokeShareLinkAsync(response.LinkId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong when revoking link.");
            }
        }

        await popupService.ClosePopupAsync(Shell.Current, false);
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    async Task Share()
    {
        ShowLoader = true;
        try
        {
            if (response is null)
                await GenerateLinkAsync();
            else
                await CopyLinkAndClose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Something went wrong when in share popup.");
        }
        finally
        {
            ShowLoader = false;
        }
    }

    private async Task CopyLinkAndClose()
    {
        await Clipboard.Default.SetTextAsync($"Hej! Udostępniam Ci nagranie nagranie westa {VideoName}. Link będzie działał przez 7 dni. Aby wyświetlić nagranie odwiedź ten link: - {response!.ShareUrl}");
        await popupService.ClosePopupAsync(Shell.Current, true);
    }

    private async Task GenerateLinkAsync()
    {
        response = await danceHttpApiClient.GetSharingLinkAsync(videoId);
        PrimaryButtonText = "Kopiuj Link";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        videoId = (Guid)query[QueryAttribute_VideoId];
        VideoName = (string)query[QueryAttribute_VideoName];
    }
}
