namespace TB.DanceDance.Mobile.Library.Services.Network;

public interface IPlatformNotification
{
    Task<bool> CheckIfNotificationPermissionsAreGranted();
    void UploadPausedNotification();
    void UploadCompleteNotification();
    void UploadProgressNotification(string fileName, int progress, long maxProgress);
    Task<bool> AskForNotificationPermission();
}