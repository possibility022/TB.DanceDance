using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;
using TB.DanceDance.Mobile.Services.DanceApi;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class UploadForegroundService : Service
{
    // This is any integer value unique to the application.
    public const int ServiceRunningNotificationId = 827364823;

    private readonly VideosDbContext dbContext;
    private readonly VideoUploader videoUploader;
    private readonly IServiceScope serviceScope;
    private CancellationTokenSource? cancellationTokenSource;
    private readonly SemaphoreSlim pauseLock = new SemaphoreSlim(0, 1);
    private bool isPaused = false;
    private NotificationChannel? notificationChannel;
    private NotificationManager? notificationManager;
    private Task? uploadingTask;
    private Task? notificationTask;

    private readonly Channel<UploadProgressEvent> channel;

    private const int notificationId = 100;
    const string channelId = "tbupload";
    const string channelName = "Uploading Dance Video";

    public static bool IsRunning = false;

    private TimeSpan delay = TimeSpan.Zero;

    public enum ServiceAction
    {
        Start,
        Stop,
        Pause
    }

    public static async Task<bool> CheckIfNotificationPermissionsAreGranted()
    {
        var res = await Permissions.CheckStatusAsync<NotificationPermission>();
        return res == PermissionStatus.Granted;
    }

    public static async Task<bool> AskForNotificationPermission()
    {
        PermissionStatus status = await Permissions.RequestAsync<NotificationPermission>();
        return status == PermissionStatus.Granted;
    }

    public UploadForegroundService()
    {
        Log.Debug("Dance Service", "UploadForegroundService Hashcode: " + GetHashCode());

        if (IPlatformApplication.Current != null)
        {
            serviceScope = IPlatformApplication.Current.Services.CreateScope();
            dbContext = serviceScope.ServiceProvider.GetRequiredService<VideosDbContext>();
            videoUploader = serviceScope.ServiceProvider.GetRequiredService<VideoUploader>();
            channel = serviceScope.ServiceProvider.GetRequiredService<Channel<UploadProgressEvent>>();
        }
        else
        {
            throw new NullReferenceException("IPlatformApplication.Current is null");
        }
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Code not directly related to publishing the notification has been omitted for clarity.
        // Normally, this method would hold the code to be run when the service is started.

        IsRunning = true;
        if (intent == null)
            throw new ArgumentNullException(nameof(intent));

        if (intent.Action == nameof(ServiceAction.Start))
        {
            RegisterNotification();
            if (isPaused)
                Resume();

            if (uploadingTask is null)
            {
                StartNewTask();
            }

            notificationTask ??= StartTaskForProgressEvents();
        }
        else if (intent.Action == nameof(ServiceAction.Stop))
        {
            cancellationTokenSource?.Cancel();
            StopForeground(StopForegroundFlags.Remove);
            StopSelfResult(startId);
        }
        else if (intent.Action == nameof(ServiceAction.Pause))
        {
            Pause();
        }

        return StartCommandResult.RedeliverIntent;
    }

    private void StartNewTask()
    {
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
            cancellationTokenSource = new CancellationTokenSource();
        Serilog.Log.Information("Starting upload Task");
        uploadingTask = Task.Run(Uploading);
    }

    private void Pause()
    {
        isPaused = true;
        cancellationTokenSource?.Cancel();
    }

    private void Resume()
    {
        isPaused = false;
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            cancellationTokenSource = new CancellationTokenSource();
            notificationTask = StartTaskForProgressEvents();
        }

        if (pauseLock.CurrentCount < 1)
            pauseLock.Release();
    }

    public static void StartService()
    {
        if (Platform.CurrentActivity is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is null");

        if (Platform.CurrentActivity.ApplicationContext is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.ApplicationContext is null");

        Intent startService = new Intent(Platform.CurrentActivity.ApplicationContext, typeof(UploadForegroundService));
        startService.SetAction(nameof(ServiceAction.Start));
        Platform.CurrentActivity.ApplicationContext.StartService(startService);
    }

    public static void StopService()
    {
        if (Platform.CurrentActivity is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is null");

        if (Platform.CurrentActivity.ApplicationContext is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.ApplicationContext is null");

        Intent startService = new Intent(Platform.CurrentActivity.ApplicationContext, typeof(UploadForegroundService));
        startService.SetAction(nameof(ServiceAction.Stop));
        Platform.CurrentActivity.ApplicationContext.StartService(startService);
    }

    public static void PauseService()
    {
        if (Platform.CurrentActivity is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is null");

        if (Platform.CurrentActivity.ApplicationContext is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.ApplicationContext is null");

        Intent startService = new Intent(Platform.CurrentActivity.ApplicationContext, typeof(UploadForegroundService));
        startService.SetAction(nameof(ServiceAction.Pause));
        Platform.CurrentActivity.ApplicationContext.StartService(startService);
    }

    private void RegisterNotification()
    {
        if (notificationChannel is null)
        {
            notificationChannel = new NotificationChannel(channelId, channelName, NotificationImportance.Min);
            notificationManager =
                (NotificationManager)Platform.AppContext.GetSystemService(NotificationService)!;
            notificationManager.CreateNotificationChannel(notificationChannel);
        }

        Notification notification = new Notification.Builder(this, channelId)
            .SetContentTitle(channelName)
            .SetSmallIcon(ResourceConstant.Drawable.abc_ab_share_pack_mtrl_alpha)
            .SetOngoing(true)
            .Build();

        StartForeground(notificationId, notification);
    }

    private async Task Uploading()
    {
        try
        {
            Serilog.Log.Information("Starting looking for videos to upload.");

            var videos = await dbContext.VideosToUpload.Where(r => r.Uploaded == false)
                .ToArrayAsync();

            foreach (var vid in videos)
            {
                await DelayIfRequired();
                await Upload(vid, cancellationTokenSource!.Token);
            }

            Serilog.Log.Information("All videos uploaded.");
        }
        catch (Exception ex)
        {
            Serilog.Log.Information(ex, "Foreground Service Exception.");
        }
        finally
        {
            StopService();
        }
    }

    private async Task DelayIfRequired()
    {
        try
        {
            if (isPaused)
            {
                await pauseLock.WaitAsync();
            }
            else
            {
                await Task.Delay(delay, cancellationTokenSource!.Token);
            }
        }
        catch (TaskCanceledException exception)
        {
            // nothing to do here
        }
    }

    private Task StartTaskForProgressEvents()
    {
        return Task.Run(() => MonitorProgress(cancellationTokenSource!.Token));
    }

    private async Task MonitorProgress(CancellationToken cancellationToken)
    {
        while (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            var message = await channel.Reader.ReadAsync(cancellationToken);

            UpdateNotification(message.FileName, message.SendBytes, message.FileSize);
        }
    }

    private void UpdateNotification(string fileName, int progress, long maxProgress)
    {
        int progressPercentage = (int)((double)progress / maxProgress * 100);

        Notification notification = new Notification.Builder(this, channelId)
            .SetContentTitle("Przesyłanie w toku.")
            .SetContentText($"Wysyłam: {fileName}")
            .SetSmallIcon(Android.Resource.Drawable.IcMenuUpload)
            .SetProgress(100, progressPercentage, false)
            .SetOngoing(true)
            .Build();

        notificationManager!.Notify(notificationId, notification);
    }

    private async Task Upload(VideosToUpload video, CancellationToken token)
    {
        try
        {
            delay = TimeSpan.Zero;
            Serilog.Log.Information("Uploading one video.");
            await videoUploader.Upload(video, token);
            video.Uploaded = true;
            Serilog.Log.Information("Video uploaded.");

            // ReSharper disable once MethodSupportsCancellation
            await dbContext.SaveChangesAsync();
        }
        catch (TaskCanceledException taskCanceledException)
        {
            // Nothing to do, wait for resume
        }
        catch (Exception ex)
        {
            delay = TimeSpan.FromMinutes(1);
            Serilog.Log.Warning(ex, "Foreground Service Exception.");
        }
    }

    public override void OnDestroy()
    {
        Log.Info("Dance Service", DateTime.Now.ToLongTimeString() + ": On Destroy");
        IsRunning = false;
        cancellationTokenSource?.Cancel();
        notificationManager?.Cancel(notificationId);
        uploadingTask = null;
        notificationTask = null;
        pauseLock.Dispose();
        serviceScope.Dispose();
        base.OnDestroy();
    }

    public static bool IsInProgress()
    {
        return IsRunning;
    }
}