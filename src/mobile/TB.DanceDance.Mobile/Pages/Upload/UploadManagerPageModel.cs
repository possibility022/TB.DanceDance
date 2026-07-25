using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Nalu;
using System.Collections.ObjectModel;
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
    private readonly IUploadStoreInitializer storeInitializer;
    private int refreshInProgress;

    public UploadManagerPageModel(
        IDbContextFactory<VideosDbContext> dbContextFactory,
        IUploadQueueService uploadQueue,
        IUploadNetworkSettings networkSettings,
        IUploadScheduler uploadScheduler,
        IUploadStoreInitializer storeInitializer)
    {
        this.dbContextFactory = dbContextFactory;
        this.uploadQueue = uploadQueue;
        this.networkSettings = networkSettings;
        this.uploadScheduler = uploadScheduler;
        this.storeInitializer = storeInitializer;
        uploadOnlyOnWiFi = networkSettings.UploadOnlyOnWiFi;
    }

    public ObservableCollection<VideosToUpload> ToUpload { get; } = [];

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
    private Task Refresh() => RefreshCore(showIndicator: true);

    public Task RefreshLiveAsync() => RefreshCore(showIndicator: false);

    private async Task RefreshCore(bool showIndicator)
    {
        if (Interlocked.Exchange(ref refreshInProgress, 1) != 0)
            return;

        try
        {
            await storeInitializer.EnsureInitializedAsync();
            if (showIndicator)
                IsRefreshing = true;

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var refreshed = await dbContext.VideosToUpload
                .OrderBy(r => r.State == UploadJobState.Completed)
                .ThenByDescending(r => r.CreatedAtUtc)
                .AsNoTracking()
                .ToListAsync();
            ApplyChanges(refreshed);
        }
        catch (Exception)
        {
            //todo - handle it
        }
        finally
        {
            if (showIndicator)
                IsRefreshing = false;
            Interlocked.Exchange(ref refreshInProgress, 0);
        }
    }

    private void ApplyChanges(IReadOnlyList<VideosToUpload> refreshed)
    {
        for (var targetIndex = 0; targetIndex < refreshed.Count; targetIndex++)
        {
            var updated = refreshed[targetIndex];
            var currentIndex = FindIndex(updated.Id);
            if (currentIndex < 0)
            {
                ToUpload.Insert(targetIndex, updated);
                continue;
            }

            if (ToUpload[currentIndex].UpdatedAtUtc != updated.UpdatedAtUtc)
                ToUpload[currentIndex] = updated;

            if (currentIndex != targetIndex)
                ToUpload.Move(currentIndex, targetIndex);
        }

        for (var index = ToUpload.Count - 1; index >= refreshed.Count; index--)
            ToUpload.RemoveAt(index);
    }

    private int FindIndex(Guid id)
    {
        for (var index = 0; index < ToUpload.Count; index++)
            if (ToUpload[index].Id == id)
                return index;

        return -1;
    }
}
