using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models;
using TB.DanceDance.Mobile.Library.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class MyVideosPageModel : ObservableObject
{
    [ObservableProperty] IReadOnlyCollection<Video> videos = [];
    [ObservableProperty] private bool isRefreshing;
    private readonly IDanceHttpApiClient apiClient;
    private readonly VideoProvider videoProvider;
    private bool videosLoaded = false;

    public MyVideosPageModel(IDanceHttpApiClient apiClient, VideoProvider videoProvider)
    {
        this.apiClient = apiClient;
        this.videoProvider = videoProvider;
    }

    [RelayCommand]
    private async Task NavigateToWatchVideo(Video video)
    {
        await Shell.Current.GoToAsync(Routes.Player, new Dictionary<string, object>()
        {
            { "videoBlobId", video.BlobId }
        });
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (!videosLoaded)
            await Refresh();
    }


    [RelayCommand]
    private async Task RenameVideo(Guid videoId)
    {
        try
        {
            var newName = await Shell.Current.CurrentPage.DisplayPromptAsync("Zmień nazwę",
                "Podaj nową nazwę dla nagrania.", "Ok",
                "Anuluj");

            if (newName == null)
                return;

            var video = Videos.First(r => r.Id == videoId);
            if (video.Name == newName)
                return;

            if (newName.Length is < 5 or > 50)
            {
                await Shell.Current.CurrentPage.DisplayAlertAsync("Zła nazwa", "Nazwa musi mieć od 5 do 50 znaków.", "Ok");
                return;
            }

            await apiClient.RenameVideoAsync(videoId, newName);
            video.Name = newName;

            await Refresh();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not rename video");
        }
    }

    [RelayCommand]
    private async Task NavigateToUpload()
    {
        await Shell.Current.GoToAsync(Routes.Upload.Uploader, new Dictionary<string, object>()
        {
            { "isPrivate", true }
        });
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            await LoadData();
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "Error when refreshing event details.");
            //todo _errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadData()
    {
        var providedVideos = await videoProvider.GetMyVideos();
        Videos = providedVideos;
        videosLoaded = true;
    }
}
