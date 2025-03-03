using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TB.DanceDance.API.Contracts.Models;
using TB.DanceDance.Mobile.Services.DanceApi;
using Video = TB.DanceDance.Mobile.Models.Video;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadManagerPageModel : ObservableObject
{
    private readonly VideosDbContext dbContext;
    private readonly DanceHttpApiClient _httpApiClient;
    private readonly VideoUploader _videoUploader;

    public UploadManagerPageModel(VideosDbContext dbContext, DanceHttpApiClient httpApiClient, VideoUploader videoUploader)
    {
        this.dbContext = dbContext;
        _httpApiClient = httpApiClient;
        _videoUploader = videoUploader;
    }

    [ObservableProperty] private ObservableCollection<Video> uploadedVideos = new();
    
    [ObservableProperty] private List<Group> groups = new();
    
    [ObservableProperty] private int selectedGroupIndex = -1;
    
    [RelayCommand]
    private async Task Appearing()
    {
        var selector = dbContext.LocalVideoUploadProgresses.Select(r => new Video { Name = r.FileName });
        foreach (Video video in selector)
        {
            UploadedVideos.Add(video);
        }

        var accesses = await _httpApiClient.GetUserAccesses();
        Groups = accesses?.Assigned.Groups.ToList() ?? [];
    }
    
    [RelayCommand]
    private async Task PickVideos()
    {
        var files = await ListVideoFiles();

        if (SelectedGroupIndex > -1)
        {
            foreach (FileResult file in files)
            {
                await _videoUploader.UploadVideoToGroup(file.FileName, file.FullPath, Groups[SelectedGroupIndex].Id,
                    CancellationToken.None);
            }
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

            return result;
        }
        catch (Exception ex)
        {
            // The user canceled or something went wrong
        }

        return Array.Empty<FileResult>();
    }
}