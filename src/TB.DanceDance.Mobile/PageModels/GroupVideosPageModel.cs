﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile.PageModels;

public partial class GroupVideosPageModel : ObservableObject
{
    private readonly VideoProvider videoProvider;
    private readonly DanceHttpApiClient apiClient;

    public GroupVideosPageModel(VideoProvider videoProvider, DanceHttpApiClient apiClient)
    {
        this.videoProvider = videoProvider;
        this.apiClient = apiClient;
    }
    
    [ObservableProperty] private bool isRefreshing;
    
    [ObservableProperty] private List<Video> videos = [];

    private bool videoLoaded = false;

    [RelayCommand]
    private async Task Appearing()
    {
        if (!videoLoaded)
            await Refresh();
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
            //_errorHandler.HandleError(e);
        }
        finally
        {
            IsRefreshing = false;
        }
    }
    
    
    [RelayCommand]
    private async Task RenameVideo(Guid videoId)
    {
        // TODO this code is duplicated in event details page model.
        // Refactor it in future.
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
                await Shell.Current.CurrentPage.DisplayAlert("Zła nazwa", "Nazwa musi mieć od 5 do 50 znaków.", "Ok");
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
    private async Task NavigateToWatchVideo(Video video)
    {
        await Shell.Current.GoToAsync(Routes.Player, new Dictionary<string, object>()
        {
            { "videoBlobId", video.BlobId }
        });
    }

    [RelayCommand]
    private async Task NavigateToUploadGroupVideo()
    {
        await Shell.Current.GoToAsync(Routes.Upload.Uploader);
    }

    private async Task LoadData()
    {
        var loadedVideos = await videoProvider.GetGroupVideosAsync();
        Videos = loadedVideos;
        videoLoaded = true;
    }
}