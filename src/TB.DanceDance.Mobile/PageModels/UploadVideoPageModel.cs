using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadVideoPageModel : ObservableObject, IQueryAttributable
{
    private readonly DanceHttpApiClient apiClient;
    private readonly VideoUploader videoUploader;

    public UploadVideoPageModel(DanceHttpApiClient apiClient, VideoUploader videoUploader)
    {
        this.apiClient = apiClient;
        this.videoUploader = videoUploader;
    }

    [ObservableProperty] private ICollection<FileResult> selectedFiles = [];

    [ObservableProperty] private List<Group> groups = new();

    [ObservableProperty] private int selectedGroupIndex = -1;
    [ObservableProperty] private bool uploadButtonEnabled = false;
    [ObservableProperty] private bool uploadButtonPressed = false;
    [ObservableProperty] private bool groupSelectorAvailable = false;
    [ObservableProperty] private Guid? eventId;

    enum UploadTo
    {
        NotSpecified,
        Event,
        Group
    }

    private UploadTo uploadTo = UploadTo.NotSpecified;

    [RelayCommand]
    private async Task Appearing()
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
    private async Task GroupSelectedIndexChanged()
    {
        SetUploadButton();
    }

    private void SetUploadButton()
    {
        if (uploadTo == UploadTo.Event)
        {
            // for event
            if (SelectedFiles.Count > 0)
            {
                UploadButtonEnabled = true;
            }
            else
            {
                UploadButtonEnabled = false;
            }
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
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(uploadTo));
                }
            }
            
            await Shell.Current.CurrentPage.DisplayAlert("Dodano", "Nagranie zostało dodane do kolejki wysyłania.",
                "OK");

            
            
            await Shell.Current.GoToAsync("..");
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

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var weHaveIt = query.TryGetValue("eventId", out object? eventIdAsObject);
        if (weHaveIt && eventIdAsObject is Guid eventIdFromRoute)
        {
            SetEventUploadStyle(eventIdFromRoute);
        }
        else
        {
            SetGroupUploadStyle();
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

    private async Task<IEnumerable<FileResult>> ListVideoFiles()
    {
        PickOptions options = new()
        {
            PickerTitle = "Please select a video file", FileTypes = FilePickerFileType.Videos,
        };

        try
        {
            var result = await FilePicker.Default.PickMultipleAsync(options);
            return result ?? [];
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }

        return [];
    }
}