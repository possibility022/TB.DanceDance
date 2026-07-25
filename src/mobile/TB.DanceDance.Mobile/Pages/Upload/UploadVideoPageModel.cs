using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nalu;
using Serilog;
using TB.DanceDance.API.Contracts.Features.Groups.Model;
using TB.DanceDance.API.Contracts.Features.Videos;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadVideoPageModel : ObservableObject,
    IAppearingAware,
    IEnteringAware<UploadVideoIntent>,
    ILeavingAware
{
    private readonly IDanceHttpApiClient apiClient;
    private readonly IUploadQueueService uploadQueue;
    private readonly INavigationService navigationService;
    private CancellationTokenSource pageLifetime = new();

    public UploadVideoPageModel(
        IDanceHttpApiClient apiClient,
        IUploadQueueService uploadQueue,
        INavigationService navigationService)
    {
        this.apiClient = apiClient;
        this.uploadQueue = uploadQueue;
        this.navigationService = navigationService;
    }

    [ObservableProperty] private ICollection<FileResult> selectedFiles = [];

    [ObservableProperty] private List<GroupModel> groups = new();

    [ObservableProperty] private int selectedGroupIndex = -1;
    [ObservableProperty] private bool uploadButtonEnabled = false;
    [ObservableProperty] private bool uploadButtonPressed = false;
    [ObservableProperty] private double stagingProgress;
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
        if (pageLifetime.IsCancellationRequested)
            pageLifetime = new CancellationTokenSource();
        try
        {
            if (uploadTo == UploadTo.Group)
            {
                var accesses = await apiClient.GetUserAccesses(pageLifetime.Token);
                Groups = accesses?.Assigned.Groups.ToList() ?? [];
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to get groups");
        }
    }

    public ValueTask OnLeavingAsync()
    {
        pageLifetime.Cancel();
        pageLifetime.Dispose();
        return ValueTask.CompletedTask;
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
            StagingProgress = 0;
            var requests = SelectedFiles.Select(CreateQueueRequest).ToArray();
            var progress = new Progress<UploadStagingProgress>(value =>
                StagingProgress = value.TotalBytes <= 0
                    ? 0
                    : Math.Clamp((double)value.CopiedBytes / value.TotalBytes, 0, 1));
            var results = await uploadQueue.EnqueueAsync(requests, progress, pageLifetime.Token);
            var succeeded = results.Count(x => x.Succeeded);
            var failed = results.Count - succeeded;

            await Shell.Current.CurrentPage.DisplayAlertAsync(
                failed == 0 ? "Dodano" : "Dodano częściowo",
                failed == 0
                    ? $"Dodano do kolejki: {succeeded}."
                    : $"Dodano do kolejki: {succeeded}. Nie udało się dodać: {failed}.",
                "OK");

            if (succeeded > 0)
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

    private UploadQueueRequest CreateQueueRequest(FileResult file) =>
        uploadTo switch
        {
            UploadTo.Event => new UploadQueueRequest(
                file.FullPath,
                SharingWithType.Event,
                EventId!.Value),
            UploadTo.Group => new UploadQueueRequest(
                file.FullPath,
                SharingWithType.Group,
                Groups[SelectedGroupIndex].Id),
            UploadTo.Private => new UploadQueueRequest(
                file.FullPath,
                SharingWithType.Private),
            _ => throw new ArgumentOutOfRangeException(nameof(uploadTo))
        };

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
