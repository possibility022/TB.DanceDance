using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using TB.DanceDance.Mobile.Services.Network;

namespace TB.DanceDance.Mobile;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class UploadForegroundService : Service, IPlatformNotification
{
    // This is any integer value unique to the application.
    public const int ServiceRunningNotificationId = 827364823;
    
    private readonly IServiceScope serviceScope;
    private CancellationTokenSource? cancellationTokenSource;
    private NotificationChannel? notificationChannel;
    private NotificationManager? notificationManager;
    private Task? uploadingTask;

    private readonly UploadWorker worker;

    private const int notificationId = 100;
    const string channelId = "tbupload";
    const string channelName = "Uploading Dance Video";
    
    public enum ServiceAction
    {
        Start,
        Stop
    }

    public async Task<bool> CheckIfNotificationPermissionsAreGranted()
    {
        var res = await Permissions.CheckStatusAsync<NotificationPermission>();
        return res == PermissionStatus.Granted;
    }

    public async Task<bool> AskForNotificationPermission()
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
            worker = serviceScope.ServiceProvider.GetRequiredService<UploadWorker>();
            worker.SetPlatformNotification(this);
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
        if (intent == null)
            throw new ArgumentNullException(nameof(intent));

        if (intent.Action == nameof(ServiceAction.Start))
        {
            RegisterNotification();

            uploadingTask ??= StartNewTask();
        }
        else if (intent.Action == nameof(ServiceAction.Stop))
        {
            cancellationTokenSource?.Cancel();
            StopForeground(StopForegroundFlags.Remove);
            StopSelfResult(startId);
        }

        return StartCommandResult.RedeliverIntent;
    }

    private Task StartNewTask()
    {
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
            cancellationTokenSource = new CancellationTokenSource();
        
        Serilog.Log.Information("Starting upload Task");
        return Task.Run(async () =>
        {
            await worker.Work(cancellationTokenSource.Token);
            uploadingTask = null;
        });
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

    public void UploadPausedNotification()
    {
        if (notificationChannel is null)
            return;
        
        Notification notification = new Notification.Builder(this, channelId)
            .SetContentTitle("Przesyłanie wstrzymane. Oczekuje na WiFi.")
            .SetSmallIcon(Android.Resource.Drawable.IcMediaPause)
            .SetOngoing(true)
            .Build();

        notificationManager!.Notify(notificationId, notification);
    }

    public void UploadProgressNotification(string fileName, int progress, long maxProgress)
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

    public override void OnDestroy()
    {
        Log.Info("Dance Service", DateTime.Now.ToLongTimeString() + ": On Destroy");
        cancellationTokenSource?.Dispose();
        notificationManager?.Cancel(notificationId);
        uploadingTask = null;
        serviceScope.Dispose();
        base.OnDestroy();
    }
}