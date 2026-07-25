using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.Work;
using Java.Util.Concurrent;
using TB.DanceDance.Mobile.Library.Services.DanceApi;
using TB.DanceDance.Mobile.Library.Services.Network;
using Application = Android.App.Application;
using Result = AndroidX.Work.ListenableWorker.Result;

namespace TB.DanceDance.Mobile;

public sealed class AndroidUploadScheduler(IUploadNetworkSettings networkSettings) : IUploadScheduler
{
    internal const string UniqueWorkName = "tb.dancedance.upload-queue";

    public Task ScheduleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var constraints = new Constraints.Builder()
            .SetRequiredNetworkType(networkSettings.UploadOnlyOnWiFi
                ? NetworkType.Unmetered
                : NetworkType.Connected)
            .Build();

        var request = new OneTimeWorkRequest.Builder(typeof(UploadBackgroundWorker))
            .SetConstraints(constraints)
            .SetBackoffCriteria(BackoffPolicy.Exponential, 10, TimeUnit.Minutes)
            .AddTag(UniqueWorkName)
            .Build();

        WorkManager.GetInstance(Application.Context)
            .EnqueueUniqueWork(UniqueWorkName, ExistingWorkPolicy.Keep, request);

        return Task.CompletedTask;
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var operation = WorkManager.GetInstance(Application.Context).CancelUniqueWork(UniqueWorkName);
        await Task.Run(() => operation.Result.Get(), cancellationToken);
    }
}

[Register("tb.dancedance.mobile.UploadBackgroundWorker")]
public sealed class UploadBackgroundWorker : Worker
{
    private const string ChannelId = "tbupload";
    private const int NotificationId = 100;
    private readonly CancellationTokenSource stopping = new();

    public UploadBackgroundWorker(Context appContext, WorkerParameters workerParameters)
        : base(appContext, workerParameters)
    {
    }

    public override Result DoWork()
    {
        try
        {
            SetForegroundAsync(CreateForegroundInfo("Preparing queued uploads", 0, 0)).Get();

            var services = IPlatformApplication.Current?.Services
                ?? throw new InvalidOperationException("MAUI services are unavailable.");
            using var scope = services.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<UploadWorker>();

            var progress = new InlineProgress<UploadProgressEvent>(message =>
            {
                var manager = (NotificationManager)ApplicationContext
                    .GetSystemService(Context.NotificationService)!;
                manager.Notify(
                    NotificationId,
                    CreateNotification(message.FileName, message.SendBytes, message.FileSize));
            });

            var result = worker.Work(progress, stopping.Token).GetAwaiter().GetResult();
            return result == UploadRunResult.Retry ? Result.InvokeRetry() : Result.InvokeSuccess();
        }
        catch (System.OperationCanceledException)
        {
            return Result.InvokeFailure();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Android upload worker failed.");
            return Result.InvokeRetry();
        }
    }

    public override void OnStopped()
    {
        stopping.Cancel();
        stopping.Dispose();
        base.OnStopped();
    }

    private ForegroundInfo CreateForegroundInfo(string fileName, long sentBytes, long totalBytes)
    {
        EnsureNotificationChannel();
        var notification = CreateNotification(fileName, sentBytes, totalBytes);
        return Build.VERSION.SdkInt >= BuildVersionCodes.Q
            ? new ForegroundInfo(
                NotificationId,
                notification,
                (int)global::Android.Content.PM.ForegroundService.TypeDataSync)
            : new ForegroundInfo(NotificationId, notification);
    }

    private Notification CreateNotification(string fileName, long sentBytes, long totalBytes)
    {
        var builder = new Notification.Builder(ApplicationContext, ChannelId)
            .SetContentTitle("Uploading dance videos")
            .SetContentText(fileName)
            .SetSmallIcon(Android.Resource.Drawable.IcMenuUpload)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true);

        if (totalBytes > 0)
        {
            var percent = (int)Math.Clamp(sentBytes * 100L / totalBytes, 0, 100);
            builder.SetProgress(100, percent, false);
        }
        else
        {
            builder.SetProgress(0, 0, true);
        }

        return builder.Build();
    }

    private void EnsureNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager)ApplicationContext
            .GetSystemService(Context.NotificationService)!;
        manager.CreateNotificationChannel(new NotificationChannel(
            ChannelId,
            "Video uploads",
            NotificationImportance.Low));
    }
}
