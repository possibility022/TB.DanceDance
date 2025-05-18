using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Services.DanceApi;
using Debug = System.Diagnostics.Debug;

namespace TB.DanceDance.Mobile;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public class UploadForegroundService : Service
{
    private readonly VideosDbContext dbContext;
    private readonly VideoUploader videoUploader;
    private readonly IServiceScope serviceScope;
    private CancellationTokenSource? cancellationTokenSource;
    
    const string channelId = "tbupload";
    const string channelName = "Uploading Dance Video";

    public enum ServiceAction
    {
        Start,
        Stop
    }

    public UploadForegroundService()
    {
        Log.Debug("Dance Service", "UploadForegroundService Hashcode: " + this.GetHashCode());

        if (IPlatformApplication.Current != null)
        {
            serviceScope = IPlatformApplication.Current.Services.CreateScope();
            this.dbContext = serviceScope.ServiceProvider.GetRequiredService<VideosDbContext>();
            this.videoUploader = serviceScope.ServiceProvider.GetRequiredService<VideoUploader>();
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
                Debug.WriteLine("Starting upload Task");
                this.uploadingTask = Task.Run(Uploading);
            }
        } else if (intent.Action == nameof(ServiceAction.Stop))
        {
            StopForeground(StopForegroundFlags.Remove);
            StopSelfResult(startId);
        }

        return StartCommandResult.RedeliverIntent;
    }

    public static void StartService()
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is null");
        
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.ApplicationContext is null)
            throw new Exception("Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.ApplicationContext is null");
        
        Intent startService = new Intent(Platform.CurrentActivity.ApplicationContext, typeof(UploadForegroundService));
        startService.SetAction(nameof(ServiceAction.Start));
        Platform.CurrentActivity.ApplicationContext.StartService(startService);
    }

    private void RegisterNotification()
    {
        NotificationChannel channel = new NotificationChannel(channelId, channelName, NotificationImportance.Max);
        NotificationManager manager = (NotificationManager)Platform.AppContext.GetSystemService(Context.NotificationService);
        manager.CreateNotificationChannel(channel);
        Notification notification = new Notification.Builder(this, channelId)
            .SetContentTitle(channelName)
            .SetSmallIcon(Resource.Drawable.abc_ab_share_pack_mtrl_alpha)
            .SetOngoing(true)
            .Build();

        StartForeground(100, notification);
    }

    private async Task Uploading()
    {
        try
        {
            Debug.WriteLine("Starting looking for videos to upload.");
            while (dbContext.VideosToUpload.FirstOrDefault(r => r.Uploaded == false) is { } video)
            {
                Debug.WriteLine("Uploading one video.");
                await videoUploader.Upload(video, cancellationTokenSource!.Token);
                video.Uploaded = true;
                Debug.WriteLine("Video uploaded.");
                await dbContext.SaveChangesAsync();
            }
            Debug.WriteLine("All videos uploaded.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Foreground Service Exception. Exception: " + ex.ToString());
        }
        finally
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
            uploadingTask = null;
        }
    }

    public override void OnDestroy()
    {
        Log.Info("Dance Service", DateTime.Now.ToLongTimeString() + ": On Destroy");
        cancellationTokenSource?.Cancel();
        uploadingTask = null;
        serviceScope?.Dispose();
        base.OnDestroy();
    }
}