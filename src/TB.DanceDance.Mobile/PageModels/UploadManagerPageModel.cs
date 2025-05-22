using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadManagerPageModel : ObservableObject
{
    private readonly VideosDbContext dbContext;

    public UploadManagerPageModel(VideosDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [ObservableProperty] private List<VideosToUpload> toUpload = [];

    [ObservableProperty] private bool uploadingInProgress;
    [ObservableProperty] private bool isRefreshing;
    
    [RelayCommand]
    private async Task UploadClicked()
    {
#if ANDROID
        try
        {
            UploadForegroundService.StartService();
            UploadingInProgress = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(3000);
                    UploadingInProgress = UploadForegroundService.IsInProgress();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Checking status failed.");
                }
            });
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error when starting upload service.");
        }
#endif
    }

    [RelayCommand]
    private async Task StopClicked()
    {
#if ANDROID
        try
        {
            UploadForegroundService.StopService();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Error when stopping upload service.");
        }
#endif
    }

    [RelayCommand]
    private async Task Appearing()
    {
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            ToUpload = await dbContext.VideosToUpload
                .OrderByDescending(r => r.Uploaded)
                .AsNoTracking()
                .ToListAsync();

#if ANDROID
            UploadingInProgress = UploadForegroundService.IsInProgress();
#endif
        }
        catch (Exception ex)
        {
            //todo - handle it
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}