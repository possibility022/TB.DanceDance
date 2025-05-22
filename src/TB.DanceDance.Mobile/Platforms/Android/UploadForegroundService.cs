using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.EntityFrameworkCore;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Data.Models.Storage;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public sealed class UploadForegroundService : Service
{
    private readonly VideosDbContext dbContext;
    private readonly VideoUploader videoUploader;
    private readonly IServiceScope serviceScope;
    private CancellationTokenSource? cancellationTokenSource;
    private NotificationChannel? notificationChannel;
    private NotificationManager? notificationManager;

    private const int notificationId = 100;
    const string channelId = "tbupload";
    const string channelName = "Uploading Dance Video";
    
    private TimeSpan delay = TimeSpan.Zero;

    public enum ServiceAction
    {
        Start,
        Stop
    }

    public UploadForegroundService()
    {
        Log.Debug("Dance Service", "UploadForegroundService Hashcode: " + GetHashCode());

        if (IPlatformApplication.Current != null)
        {
            serviceScope = IPlatformApplication.Current.Services.CreateScope();
            dbContext = serviceScope.ServiceProvider.GetRequiredService<VideosDbContext>();
            videoUploader = serviceScope.ServiceProvider.GetRequiredService<VideoUploader>();
        }
        else
        {
            throw new NullReferenceException("IPlatformApplication.Current is null");
        }
    }
    
    // This is any integer value unique to the application.
    public const int ServiceRunningNotificationId = 827364823;
    private Task? uploadingTask;

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    [return:GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Code not directly related to publishing the notification has been omitted for clarity.
        // Normally, this method would hold the code to be run when the service is started.

        if (intent == null)
            throw new ArgumentNullException(nameof(intent));
        
        if (intent.Action == nameof(ServiceAction.Start))
        {
            RegisterNotification();
            if (cancellationTokenSource?.IsCancellationRequested != false
                && uploadingTask == null)
            {
                if (cancellationTokenSource is null || cancellationTokenSource?.IsCancellationRequested == true)
                    cancellationTokenSource = new CancellationTokenSource();
                Serilog.Log.Information("Starting upload Task");
                uploadingTask = Task.Run(Uploading);
            }
        } else if (intent.Action == nameof(ServiceAction.Stop))
        {
            cancellationTokenSource?.Cancel();
            StopForeground(StopForegroundFlags.Remove);
            StopSelfResult(startId);
        }

        return StartCommandResult.RedeliverIntent;
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

    private async Task Uploading()
    {
        try
        {
            Serilog.Log.Information("Starting looking for videos to upload.");
            
            var videos = await dbContext.VideosToUpload.Where(r => r.Uploaded == false)
                .ToArrayAsync();

            for (int index = 0; index < videos.Length; index++)
            {
                await Task.Delay(delay);
                if (cancellationTokenSource!.IsCancellationRequested)
                    break;
                
                await Upload(videos[index]);
                await UpdateNotification(index + 1, videos.Length, 0, 0);
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

    private async Task UpdateNotification(int currentFile, int filesCount, int progress, int maxProgress)
    {
        await Task.Delay(500);

        Notification notification = new Notification.Builder(this, channelId)
            .SetContentTitle("Przesyłanie w toku")
            .SetContentText($"Postęp: {currentFile}/{filesCount}")
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            //.SetProgress(maxProgress, progress, false)
            .SetOngoing(true)
            .Build();

        notificationManager!.Notify(notificationId, notification);
    }

    private async Task Upload(VideosToUpload video)
    {
        try
        {
            delay = TimeSpan.Zero;
            Serilog.Log.Information("Uploading one video.");
            await videoUploader.Upload(video, cancellationTokenSource!.Token);
            video.Uploaded = true;
            Serilog.Log.Information("Video uploaded.");
            await dbContext.SaveChangesAsync();
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
        cancellationTokenSource?.Cancel();
        notificationManager?.Cancel(notificationId);
        uploadingTask = null;
        serviceScope.Dispose();
        base.OnDestroy();
    }
}