using Android.App;
using Android.Content;
using Android.OS;
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

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        // Code not directly related to publishing the notification has been omitted for clarity.
        // Normally, this method would hold the code to be run when the service is started.

        if (intent == null)
            throw new ArgumentNullException(nameof(intent));
        
        if (intent.Action == nameof(ServiceAction.Start))
        {
            RegisterNotification();
            // if (cancellationTokenSource?.IsCancellationRequested != false
            //     && uploadingTask == null
            //    )
            //     uploadingTask = Uploading();
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
    }

    private async Task Uploading()
    {
        try
        {
            if (cancellationTokenSource is null || cancellationTokenSource?.IsCancellationRequested == true)
                cancellationTokenSource = new CancellationTokenSource();

            VideosToUpload? video = null;
            while ((video = dbContext.VideosToUpload.FirstOrDefault(r => r.Uploaded == false)) != null)
            {
                await videoUploader.Upload(video, cancellationTokenSource!.Token);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
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
        uploadingTask?.Dispose();
        uploadingTask = null;
        serviceScope?.Dispose();
        base.OnDestroy();
    }
}