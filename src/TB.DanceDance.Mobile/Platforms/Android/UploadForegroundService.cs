using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using TB.DanceDance.Mobile.Data;
using TB.DanceDance.Mobile.Services.DanceApi;

namespace TB.DanceDance.Mobile;

[Service(ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public class UploadForegroundService : Service
{
    private readonly VideosDbContext dbContext;
    private readonly VideoUploader videoUploader;
    private readonly IServiceScope serviceScope;
    private CancellationTokenSource? cancellationTokenSource;

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

    public override void OnCreate()
    {
        base.OnCreate();
        if (cancellationTokenSource?.IsCancellationRequested == true)
            cancellationTokenSource = new CancellationTokenSource();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Code not directly related to publishing the notification has been omitted for clarity.
        // Normally, this method would hold the code to be run when the service is started.

        var notification = new Notification.Builder(this, "tb.dancedance.app")
            .SetContentTitle("Uploading Videos")
            .SetContentText("Videos content :)")
            .SetSmallIcon(Resource.Drawable.abc_ic_arrow_drop_right_black_24dp)
            //.SetContentIntent(BuildIntentToShowMainActivity())
            .SetOngoing(true)
            //.AddAction(BuildRestartTimerAction())
            //.AddAction(BuildStopServiceAction())
            .Build();

        // Enlist this instance of the service as a foreground service
        StartForeground(ServiceRunningNotificationId, notification);
        if (cancellationTokenSource?.IsCancellationRequested == false
            && uploadingTask == null
            )
            uploadingTask = Uploading();

        return StartCommandResult.RedeliverIntent;
    }

    private async Task Uploading()
    {
        VideosToUpload? video = null;
        while ((video = dbContext.VideosToUpload.FirstOrDefault(r => r.Uploaded == false)) != null)
        {
            await videoUploader.UploadVideoToGroup(video.FileName, video.FullFileName, Guid.Empty, cancellationTokenSource!.Token);
        }
    }

    public override void OnDestroy()
    {
        Log.Info("Dance Service", DateTime.Now.ToLongTimeString() + ": On Destroy");
        cancellationTokenSource?.Cancel();
        uploadingTask?.Dispose();
        uploadingTask = null;
        serviceScope?.Dispose();
        base.OnDestroy();
    }
}