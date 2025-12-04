using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.PageModels;

public partial class UploadManagerPageModel : ObservableObject
{
    private readonly VideosDbContext dbContext;
    private readonly IPlatformNotification platformNotification;

    public UploadManagerPageModel(VideosDbContext dbContext, IPlatformNotification platformNotification)
    {
        this.dbContext = dbContext;
        this.platformNotification = platformNotification;
    }

    [ObservableProperty] private List<VideosToUpload> toUpload = [];

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
        NotificationBlocked = !(await platformNotification.CheckIfNotificationPermissionsAreGranted());
    }

    [RelayCommand]
    private async Task AskNotificationPermissions()
    {
        NotificationBlocked = !(await platformNotification.AskForNotificationPermission());
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