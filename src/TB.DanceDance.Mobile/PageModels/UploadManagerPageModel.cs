using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadManagerPageModel : ObservableObject
{
    private readonly VideosDbContext _dbContext;

    public UploadManagerPageModel(VideosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [ObservableProperty] private ObservableCollection<Video> _uploadedVideos = new();
    
    [RelayCommand]
    private async Task Appearing()
    {
        var selector = _dbContext.LocalVideoUploadProgresses.Select(r => new Video { Name = r.Filename });
        foreach (Video video in selector)
        {
            UploadedVideos.Add(video);
        }
    }
    
    [RelayCommand]
    private async Task PickVideos()
    {
        var files = await ListVideoFiles();
        foreach (var file in files)
        {
            UploadedVideos.Add(new Video() { Name = file.FileName });
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

        return null;
    }
}

public class Video
{
    public string Name { get; set; }
}