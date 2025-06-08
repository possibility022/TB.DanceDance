using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadGroupVideoPageModel : ObservableObject
{
    private readonly DanceHttpApiClient apiClient;
    private readonly VideoUploader videoUploader;


    public UploadGroupVideoPageModel(DanceHttpApiClient apiClient, VideoUploader videoUploader)
    {
        this.apiClient = apiClient;
        this.videoUploader = videoUploader;
    }
    
    
    [ObservableProperty] private ICollection<FileResult> selectedFiles = [];
    
    [ObservableProperty] private List<Group> groups = new();
    
    [ObservableProperty] private int selectedGroupIndex = -1;
    [ObservableProperty] private bool uploadButtonEnabled = false;
    
    [RelayCommand]
    private async Task Appearing()
    {
        try
        {
            var accesses = await apiClient.GetUserAccesses();
            Groups = accesses?.Assigned.Groups.ToList() ?? [];
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to get groups");
        }
    }
    
    [RelayCommand]
    private async Task PickVideos()
    {
        var files = await ListVideoFiles();
        
        SelectedFiles = files.ToList();
        if (SelectedFiles.Count > 0 && SelectedGroupIndex > -1)
        {
            UploadButtonEnabled = true;
        }
    }

    [RelayCommand]
    private async Task UploadSelectedVideos()   
    {
        if (SelectedGroupIndex > -1)
        {
            foreach (FileResult file in SelectedFiles)
            {
                await videoUploader.AddToUploadList(file.FileName, file.FullPath, Groups[SelectedGroupIndex].Id,
                    CancellationToken.None);
            }
            
            await Shell.Current.GoToAsync("//UploadManagerPage");
        }
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
            return result ?? []; // it can be null for some reason
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }

        return [];
    }
}