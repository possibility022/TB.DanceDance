using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Pages.Upload;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadVideoPageModel : ObservableObject,
    IAppearingAware,
    IEnteringAware<UploadVideoIntent>
{
    private readonly IDanceHttpApiClient apiClient;
    private readonly IVideoUploader videoUploader;
    private readonly INavigationService navigationService;

    public UploadVideoPageModel(IDanceHttpApiClient apiClient, IVideoUploader videoUploader, INavigationService navigationService)
    {
        this.apiClient = apiClient;
        this.videoUploader = videoUploader;
        this.navigationService = navigationService;
    }

    [ObservableProperty] private ICollection<FileResult> selectedFiles = [];

    [ObservableProperty] private List<GroupModel> groups = new();

    [ObservableProperty] private int selectedGroupIndex = -1;
    [ObservableProperty] private bool uploadButtonEnabled = false;
    [ObservableProperty] private bool uploadButtonPressed = false;
    [ObservableProperty] private bool groupSelectorAvailable = false;
    [ObservableProperty] private Guid? eventId;

    enum UploadTo
    {
        NotSpecified,
        Event,
        Group,
        Private
    }

    private UploadTo uploadTo = UploadTo.NotSpecified;

    public ValueTask OnEnteringAsync(UploadVideoIntent intent)
    {
        switch (intent)
        {
            case UploadToPrivateIntent:
                SetPrivateUploadStyle();
                break;
            case UploadToEventIntent e:
                SetEventUploadStyle(e.EventId);
                break;
            case UploadToGroupIntent:
                SetGroupUploadStyle();
                break;
        }

        return default;
    }

    public async ValueTask OnAppearingAsync()
    {
        try
        {
            if (uploadTo == UploadTo.Group)
            {
                var accesses = await apiClient.GetUserAccesses();
                Groups = accesses?.Assigned.Groups.ToList() ?? [];
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to get groups");
        }
    }

    [RelayCommand]
    private void GroupSelectedIndexChanged()
    {
        SetUploadButton();
    }

    private void SetUploadButton()
    {
        if (uploadTo == UploadTo.Event || uploadTo == UploadTo.Private)
        {
            UploadButtonEnabled = SelectedFiles.Count > 0;
        }
        else if (SelectedFiles.Count > 0 && SelectedGroupIndex > -1)
        {
            UploadButtonEnabled = true;
        }
    }

    [RelayCommand]
    private async Task PickVideos()
    {
        var files = await ListVideoFiles();
        SelectedFiles = files.ToList();
        SetUploadButton();
    }

    [RelayCommand]
    private async Task UploadSelectedVideos()
    {
        try
        {
            UploadButtonPressed = true;
            UploadButtonEnabled = false;
            foreach (FileResult file in SelectedFiles)
            {
                if (uploadTo == UploadTo.Event)
                {
                    await videoUploader.UploadVideoToEvent(file.FullPath, EventId!.Value,
                        CancellationToken.None); //todo cancellation token
                }
                else if (uploadTo == UploadTo.Group)
                {
                    await videoUploader.UploadVideoToGroup(file.FullPath, Groups[SelectedGroupIndex].Id,
                        CancellationToken.None);
                }
                else if (uploadTo == UploadTo.Private)
                {
                    await videoUploader.UploadVideoToPrivate(file.FullPath, CancellationToken.None);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(uploadTo));
                }
            }

            await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Dodano", "Nagranie zostało dodane do kolejki wysyłania.",
                "OK");

            await navigationService.GoToAsync(Navigation.Relative().Pop());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not upload videos.");
        }
        finally
        {
            UploadButtonEnabled = true;
            UploadButtonPressed = false;
        }
    }

    private void SetGroupUploadStyle()
    {
        GroupSelectorAvailable = true;
        EventId = null;
        uploadTo = UploadTo.Group;
    }

    private void SetEventUploadStyle(Guid eventIdFromRoute)
    {
        GroupSelectorAvailable = false;
        EventId = eventIdFromRoute;
        uploadTo = UploadTo.Event;
    }

    private void SetPrivateUploadStyle()
    {
        GroupSelectorAvailable = false;
        uploadTo = UploadTo.Private;
    }

    private async Task<IEnumerable<FileResult>> ListVideoFiles()
    {
        PickOptions options = new()
        {
            PickerTitle = "Please select a video file",
            FileTypes = FilePickerFileType.Videos,
        };

        try
        {
            var result = await FilePicker.Default.PickMultipleAsync(options);
            return result ?? [];
        }
        catch (Exception)
        {
            // The user canceled or something went wrong
        }

        return [];
    }
}
