using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Nalu;
using TB.DanceDance.Mobile.Library.Data;
using TB.DanceDance.Mobile.Library.Data.Models.Storage;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;

namespace TB.DanceDance.Mobile.Pages.Upload;

public partial class UploadManagerPageModel : ObservableObject, IAppearingAware
{
    private readonly IDbContextFactory<VideosDbContext> dbContextFactory;
    private readonly IUploadQueueService uploadQueue;
    private readonly IUploadNetworkSettings networkSettings;
    private readonly IUploadScheduler uploadScheduler;

    public UploadManagerPageModel(
        IDbContextFactory<VideosDbContext> dbContextFactory,
        IUploadQueueService uploadQueue,
        IUploadNetworkSettings networkSettings,
        IUploadScheduler uploadScheduler)
    {
        this.dbContextFactory = dbContextFactory;
        this.uploadQueue = uploadQueue;
        this.networkSettings = networkSettings;
        this.uploadScheduler = uploadScheduler;
        uploadOnlyOnWiFi = networkSettings.UploadOnlyOnWiFi;
    }

    [ObservableProperty] private List<VideosToUpload> toUpload = [];

    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool notificationBlocked;
    [ObservableProperty] private bool uploadOnlyOnWiFi;

    public async ValueTask OnAppearingAsync()
    {
        await Refresh();
        await CheckNotificationSettings();
    }

    private async Task CheckNotificationSettings()
    {
#if ANDROID
        NotificationBlocked =
            await Permissions.CheckStatusAsync<NotificationPermission>() != PermissionStatus.Granted;
#else
        NotificationBlocked = false;
#endif
    }

    [RelayCommand]
    private async Task AskNotificationPermissions()
    {
#if ANDROID
        NotificationBlocked =
            await Permissions.RequestAsync<NotificationPermission>() != PermissionStatus.Granted;
#else
        NotificationBlocked = false;
#endif
    }

    async partial void OnUploadOnlyOnWiFiChanged(bool value)
    {
        try
        {
            networkSettings.UploadOnlyOnWiFi = value;
            await uploadScheduler.CancelAsync();
            await uploadScheduler.ScheduleAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Could not apply upload network setting.");
        }
    }

    [RelayCommand]
    private async Task Retry(VideosToUpload job)
    {
        await uploadQueue.RetryAsync(job.Id);
        await Refresh();
    }

    [RelayCommand]
    private async Task Cancel(VideosToUpload job)
    {
        await uploadQueue.CancelAsync(job.Id);
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        try
        {
            IsRefreshing = true;
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            ToUpload = await dbContext.VideosToUpload
                .OrderBy(r => r.State == UploadJobState.Completed)
                .ThenByDescending(r => r.CreatedAtUtc)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception)
        {
            //todo - handle it
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public Task RefreshLiveAsync() => IsRefreshing ? Task.CompletedTask : Refresh();
}
