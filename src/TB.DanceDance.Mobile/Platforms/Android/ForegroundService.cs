using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace TB.DanceDance.Mobile;

public static class ForegroundService
{
    // public static void StartForegroundServiceCompat<T>(this Context context, Bundle args = null) where T : Service
    // {
    //     var intent = new Intent(context, typeof(T));
    //     if (args != null) 
    //     {
    //         intent.PutExtras(args);
    //     }
    //
    //     if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
    //     {
    //         context.StartForegroundService(intent);
    //     }
    //     else
    //     {
    //         context.StartService(intent);
    //     }
    // }
    //
    // // This is any integer value unique to the application.
    // public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
    //
    // public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    // {
    //     // Code not directly related to publishing the notification has been omitted for clarity.
    //     // Normally, this method would hold the code to be run when the service is started.
    //
    //     var notification = new Notification.Builder(this)
    //         .SetContentTitle(Resources.GetString(Resource.String.app_name))
    //         .SetContentText(Resources.GetString(Resource.String.notification_text))
    //         .SetSmallIcon(Resource.Drawable.ic_stat_name)
    //         .SetContentIntent(BuildIntentToShowMainActivity())
    //         .SetOngoing(true)
    //         .AddAction(BuildRestartTimerAction())
    //         .AddAction(BuildStopServiceAction())
    //         .Build();
    //
    //     // Enlist this instance of the service as a foreground service
    //     StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
    // }
}