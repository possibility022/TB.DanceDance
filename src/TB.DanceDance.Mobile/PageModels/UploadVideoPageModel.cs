using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    }

    [RelayCommand]
    private async Task PickVideos()
    {
        var files = await ListVideoFiles();
        SelectedFiles = files.ToList();
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
                        throw new NotImplementedException();
                        // todo handle upload to groups
                        await videoUploader.AddToUploadList(file.FileName, file.FullPath, Groups[SelectedGroupIndex].Id,
                            CancellationToken.None);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(uploadTo));
                    }
                }

                await Shell.Current.GoToAsync("//UploadManagerPage");
            }
            finally
            {
                UploadButtonEnabled = true;
                UploadButtonPressed = false;
            }
        
    }

    [ObservableProperty] private Guid? eventId;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var weHaveIt = query.TryGetValue("eventId", out object? eventIdAsObject);
        if (weHaveIt && eventIdAsObject is Guid eventIdFromRoute)
        {
            SetEventUploadStyle(eventIdFromRoute);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void SetEventUploadStyle(Guid eventId)
    {
        EventId = eventId;
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
            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }

        return [];
    }
}