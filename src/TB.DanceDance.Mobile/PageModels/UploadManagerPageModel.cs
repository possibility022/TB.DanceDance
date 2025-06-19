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
    [ObservableProperty] private bool notificationBlocked;

    [RelayCommand]
    private async Task Appearing()
    {
        await Refresh();
        await CheckNotificationSettings();
    }

    private async Task CheckNotificationSettings()
    {
#if ANDROID
        NotificationBlocked = !(await UploadForegroundService.CheckIfNotificationPermissionsAreGranted());
#endif
    }

    [RelayCommand]
    private async Task AskNotificationPermissions()
    {
#if ANDROID
        NotificationBlocked = !(await UploadForegroundService.AskForNotificationPermission());
#endif
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